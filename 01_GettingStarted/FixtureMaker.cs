using System.Numerics;
using PicoGK;

namespace Fixture
{
    public class FixtureMakerApp
    {
        public static void Run()
        {
            Fixture.BasePlate oBase = new(new(300, 200), // vecSizeMM
                                            20, // fHoleSpacingMM
                                            8 // fHoleDiameterMM
            );

            Mesh mshSmall = Mesh.mshFromStlFile(Path.Combine(
                Utils.strPicoGKSourceCodeFolder(),
                // "../asset/instaxmini_to_800_adapter_V11_1_body.stl"));
                "../asset/Teapot.stl"));

            Mesh mshObject = mshSmall.mshCreateTransformed(new Vector3(6, 6, 6), Vector3.Zero);
            // Mesh mshObject = mshSmall.mshCreateTransformed(new Vector3(1, 1, 1), Vector3.Zero);

            Object oObject = new(mshObject,
                                10, // fObjectBottomMM
                                22, // fSleeveMM
                                8, // fWallMM
                                25, // fFlangeMM
                                .2f // tolarance
            );

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
                                       float fFlangeMM,
                                       float fObjectTolerance = 0.1f)
        {
            // param valiadate
            if (fObjectTolerance < 0f)
                throw new Exception("Object tolerance must be equal or larger than 0");

            m_fTolerance = fObjectTolerance;

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

        public float fObjectTolerance()
        {
            return m_fTolerance;
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
        float m_fTolerance;
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

            // create mounting holes
            if (!oPlate.bDoesFit(voxFlange))
                throw new Exception("Flange doesn't fit onto base plate");

            voxFlange = oPlate.voxCreateMountableFlange(voxFlange);

            m_voxFixture.BoolAdd(voxFlange);

            // z slice upward, gives tolarence for removable
            Voxels voxObjectRemovable = new(oObject.voxObject());
            voxObjectRemovable.Offset(oObject.fObjectTolerance());
            m_voxFixture.OverOffset(-1);

            BBox3 oObjectBounds = voxObjectRemovable.mshAsMesh().oBoundingBox();
            voxObjectRemovable.ProjectZSlice(oObjectBounds.vecMin.Z, oObjectBounds.vecMax.Z);

            m_voxFixture -= voxObjectRemovable;

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
            public BasePlate(Vector2 vecSizeMM,
                        float fHoleSpacingMM,
                        float fHoleDiameterMM)
            {
                m_vecSize = vecSizeMM;
                m_fHoleSpacing = fHoleSpacingMM;
                m_fHoleRadius = fHoleDiameterMM / 2;
            }
            public bool bDoesFit(Voxels voxFlange)
            {
                BBox3 oBox = voxFlange.mshAsMesh().oBoundingBox();

                // TODO try the other way around, maybe it fits 90º rotated
                Console.WriteLine("Size Debug");
                Console.WriteLine(oBox.vecSize().X);
                Console.WriteLine(m_vecSize.X);
                Console.WriteLine(oBox.vecSize().Y);
                Console.WriteLine(m_vecSize.Y);

                if (oBox.vecSize().X > m_vecSize.X)
                    return false;

                if (oBox.vecSize().Y > m_vecSize.Y)
                    return false;

                return true;
            }

            public Voxels voxCreateMountableFlange(in Voxels voxFlange) // in : passed as read only, dangerous aspects of C#
            {
                // drill holes and return
                if (!bDoesFit(voxFlange))
                    throw new Exception("Flange doesn't fit onto base plate");

                BBox3 oBox = voxFlange.mshAsMesh().oBoundingBox();

                // We use the extent of the flange for the region where we drill holes
                // as it doesn't make much sense to drill outside

                int nXCount = (int)float.Ceiling(oBox.vecSize().X / m_fHoleSpacing) + 1;
                int nYCount = (int)float.Ceiling(oBox.vecSize().Y / m_fHoleSpacing) + 1;

                // We use the center of the flange as the reference, so the object is centered
                // in the middle of the base plate
                Vector3 vecOrigin = oBox.vecCenter() - new Vector3(m_fHoleSpacing * (nXCount / 2),
                                                                        m_fHoleSpacing * (nYCount / 2),
                                                                        0);

                // Drill beam begin and end
                Vector3 vecBegin = vecOrigin;
                Vector3 vecEnd = vecOrigin;

                // Modify the Z coordinate, so we get a nice
                // drill that cuts through the entire flange
                // we add a millimeter to both sides that
                // we get a clean cut, just in case

                vecBegin.Z = oBox.vecMax.Z + 1;
                vecEnd.Z = oBox.vecMin.Z - 1;

                // Now we have a drill vector that starts at the origin of the base plate
                // Let's create the lattice with all the drill beams

                Lattice latDrills = new();

                for (int x = 0; x < nXCount; x++)
                {
                    // Reset the Y coordinate for every row we drill
                    vecBegin.Y = vecOrigin.Y;
                    vecEnd.Y = vecOrigin.Y;

                    for (int y = 0; y < nYCount; y++)
                    {
                        latDrills.AddBeam(vecBegin, vecEnd, m_fHoleRadius, m_fHoleRadius);

                        vecBegin.Y += m_fHoleSpacing;
                        vecEnd.Y += m_fHoleSpacing;
                    }

                    vecBegin.X += m_fHoleSpacing;
                    vecEnd.X += m_fHoleSpacing;
                }

                // Voxelize the lattice and drill the holes

                Voxels voxDrills = new(latDrills);

                Voxels voxDrilledFlange = new(voxFlange);
                voxDrilledFlange.BoolSubtract(voxDrills);

                // And we have a perforated flange
                return voxDrilledFlange;
            }

            Vector2 m_vecSize;
            float m_fHoleSpacing;
            float m_fHoleRadius;
        }

        public Voxels voxAsVoxels()
        {
            return m_voxFixture;
        }

        Voxels m_voxFixture;
    }
}
