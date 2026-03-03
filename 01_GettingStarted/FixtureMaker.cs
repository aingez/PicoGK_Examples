using System.Numerics;
using PicoGK;

namespace Fixture
{
    public class FixtureMakerApp
    {
        public static void Run()
        {
            Fixture.BasePlate oBase = new();

            Mesh mshSmall = Mesh.mshFromStlFile(Path.Combine(
                Utils.strPicoGKSourceCodeFolder(),
                // "../asset/instaxmini_to_800_adapter_V11_1_body.stl"));
                "../asset/Teapot.stl"));

            Mesh mshObject = mshSmall.mshCreateTransformed(new Vector3(6, 6, 6), Vector3.Zero);

            Object oObject = new(mshObject,
                                                    15,
                                                    20,
                                                    5,
                                                    25);

            // create new fixture and export as stl
            // passing a ProgressReporterActive object, which routes all of our information to the viewer
            Fixture oFixture = new(oBase,
                        oObject,
                        new ProgressReporterActive()); // verbose
                                                       // new ProgressReporterSilent());
            oFixture.voxAsVoxels().mshAsMesh().SaveToStlFile(Path.Combine(Utils.strDocumentsFolder(),
                                                                "Fixture.stl"));
        }
    }

    public class ProgressReporterActive : ProgressReporter
    {
        public override void AddObject(Voxels vox,
                                        int iGroupID = 0)
        {
            Library.oViewer().Add(vox, iGroupID);
        }

        public override void AddObject(Mesh msh,
                                        int iGroupID = 0)
        {
            Library.oViewer().Add(msh, iGroupID);
        }

        public override void SetGroupMaterial(int iID,
                                                ColorFloat clr,
                                                float fMetallic,
                                                float fRoughness)
        {
            Library.oViewer().SetGroupMaterial(iID,
                                                clr,
                                                fMetallic,
                                                fRoughness);
        }

        public override void ReportTask(string strTask)
        {
            Library.Log(strTask);
        }

    }
    public class ProgressReporterSilent : ProgressReporter
    {
        public override void AddObject(Voxels vox,
                                        int iGroupID = 0)
        { }

        public override void AddObject(Mesh msh,
                                        int iGroupID = 0)
        { }

        public override void SetGroupMaterial(int iID,
                                                ColorFloat clr,
                                                float fMetallic,
                                                float fRoughness)
        { }

        public override void ReportTask(string strTask)
        { }

    }

    public abstract class ProgressReporter
    {
        public abstract void AddObject(Voxels vox,
                                        int iGroupID = 0);
        public abstract void AddObject(Mesh msh,
                                        int iGroupID = 0);
        public abstract void SetGroupMaterial(int iID,
                                                ColorFloat clr,
                                                float fMetallic,
                                                float fRoughness);
        public abstract void ReportTask(string strTask);

    }

    public class Object
    {
        public Object(Mesh msh,
                                       float fObjectBottomMM,
                                       float fSleeveMM,
                                       float fWallMM,
                                       float fFlangeMM)
        {
            // param valiadate
            if (fObjectBottomMM <= 0)
                throw new Exception("Object cannot vbe placed under build plate.");

            // set bound
            BBox3 oObjectBounds = msh.oBoundingBox();
            Vector3 vecOffset = new Vector3(
                -oObjectBounds.vecCenter().X,
                -oObjectBounds.vecCenter().Y,
                -oObjectBounds.vecMin.Z + fObjectBottomMM
            );

            m_voxObject = new Voxels(msh.mshCreateTransformed(Vector3.One, vecOffset));
            m_fObjectBottom = fObjectBottomMM;
            m_fSleeve = fSleeveMM;
            m_fWall = fWallMM;
            m_fFlange = fFlangeMM;
        }

        public Voxels voxObject()
        {
            return m_voxObject;
        }

        public float fWallMM()
        {
            return m_fWall;
        }

        public float fSleeveMM()
        {
            return m_fSleeve;
        }

        public float fFlangeHeightMM()
        {
            return m_fObjectBottom;
        }

        public float fFlangeWidthMM()
        {
            return m_fFlange;
        }

        Voxels m_voxObject;
        float m_fObjectBottom;
        float m_fSleeve;
        float m_fWall;
        float m_fFlange;
    }

    public class Fixture
    {
        public Fixture(BasePlate oPlate,
                        Object oObject,
                        ProgressReporter oProgress)
        {
            // new mesh import
            oProgress.ReportTask("Creating a new fixture");
            m_voxFixture = new(oObject.voxObject());
            oProgress.ReportTask("Creating the sleeve");

            m_voxFixture.Offset(oObject.fWallMM());
            m_voxFixture.ProjectZSlice(oObject.fFlangeHeightMM() + oObject.fSleeveMM(), 0);

            BBox3 oFixtureBounds = m_voxFixture.mshAsMesh().oBoundingBox();
            oFixtureBounds.vecMin.Z = 0;
            oFixtureBounds.vecMax.Z = oObject.fFlangeHeightMM() + oObject.fSleeveMM();

            Mesh mshIntersect = Utils.mshCreateCube(oFixtureBounds);
            m_voxFixture.BoolIntersect(new Voxels(mshIntersect));

            // Flange
            oProgress.ReportTask("Building the flange");
            Voxels voxFlange = new(m_voxFixture);
            voxFlange.Offset(oObject.fFlangeWidthMM());

            BBox3 oFlangeBounds = voxFlange.mshAsMesh().oBoundingBox();
            oFlangeBounds.vecMin.Z = 0;
            oFlangeBounds.vecMax.Z = oObject.fFlangeHeightMM();

            Mesh mshIntersectFlange = Utils.mshCreateCube(oFlangeBounds);
            voxFlange.BoolIntersect(new Voxels(mshIntersectFlange));

            m_voxFixture.BoolAdd(voxFlange);

            // z slice upward
            Voxels voxObjectRemovable = new(oObject.voxObject());
            BBox3 oObjectBounds = voxObjectRemovable.mshAsMesh().oBoundingBox();
            voxObjectRemovable.ProjectZSlice(oObjectBounds.vecMin.Z,
                                                oObjectBounds.vecMax.Z);

            m_voxFixture.BoolSubtract(voxObjectRemovable);

            // progress report
            oProgress.SetGroupMaterial(0, "da9c6b", 0.3f, 0.7f);
            oProgress.SetGroupMaterial(1, "FF0000BB", 0.5f, 0.5f);
            oProgress.SetGroupMaterial(2, "0000FF33", 0.5f, 0.5f);

            oProgress.AddObject(oObject.voxObject(), 1);
            oProgress.AddObject(voxObjectRemovable, 2);
            oProgress.AddObject(m_voxFixture, 0);

        }

        public class BasePlate
        {
            // nothing here yet
        }

        public Voxels voxAsVoxels()
        {
            return m_voxFixture;
        }

        Voxels m_voxFixture;
    }
}
