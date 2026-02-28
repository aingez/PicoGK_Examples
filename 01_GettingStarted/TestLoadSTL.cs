using PicoGK;
using System.Numerics;

namespace PicoGKExamples
{
    class TestLoadSTL
    {
        public static void Run()
        {
            Mesh mshSmall = Mesh.mshFromStlFile(
                Path.Combine(
                    Utils.strPicoGKSourceCodeFolder(),
                    "../asset/instaxmini_to_800_adapter_V11_1_body.stl"
                )
            );

            Mesh mshObject = mshSmall.mshCreateTransformed(new(6, 6, 6), Vector3.Zero);

            Library.oViewer().Add(mshObject);
        }
    }
}