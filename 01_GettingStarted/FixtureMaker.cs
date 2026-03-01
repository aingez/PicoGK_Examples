using System.Numerics;
using PicoGK;

namespace Fixture
{
    public class App
    {
        public static void Run()
        {
            BasePlate oBase = new();

            Mesh mshSmall = Mesh.mshFromStlFile(Path.Combine(
                Utils.strPicoGKSourceCodeFolder(),
                "../asset/instaxmini_to_800_adapter_V11_1_body.stl"));

            Mesh mshObject = mshSmall.mshCreateTransformed(new Vector3(6, 6, 6), Vector3.Zero);

            FixtureObject oObject = new(
                mshObject,
                10,
                15,
                3,
                20);

            FixtureMaker oMaker = new(oBase, oObject);
            oMaker.Run();
        }
    }

    public class FixtureObject
    {

        public FixtureObject(Mesh msh,
                            float fSleeveMM,
                            float fWallMM,
                            float fFlangeMM,
                            float fObjectBottomMM)
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

    public class BasePlate
    {
        // nothing here yet
    }

    public class FixtureMaker
    {
        public FixtureMaker( // constructure
            BasePlate oPlate,
            FixtureObject oObject
        )
        {
            m_oPlate = oPlate;
            m_oObject = oObject;
        }

        public void Run()
        {
            // BasePlate oBase = new();
            // FixtureObject oObject = new();

            // FixtureMaker oMaker = new(oBase, oObject);
            // oMaker.Run();

            Voxels voxFixture = new(m_oObject.voxObject());
            voxFixture.Offset(m_oObject.fWallMM());
            voxFixture.ProjectZSlice(m_oObject.fFlangeHeightMM() + m_oObject.fSleeveMM(), 0);

            // view result
            Library.oViewer().Add(voxFixture);
        }

        BasePlate m_oPlate;
        FixtureObject m_oObject;
    }
}
