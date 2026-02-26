using PicoGK;
using System.Numerics;

namespace PicoGKExamples
{
    class TestLoop
    {
        public static void Run()
        {
            Lattice lat = new();
            Vector3 vecPrevious = new(0, 0, 0);
            Random oRand = new();

            for (int n = 0; n < 300; n++)
            {
                Vector3 vecNew = new(oRand.NextSingle() * 100,
                                        oRand.NextSingle() * 100,
                                        oRand.NextSingle() * 100);

                lat.AddBeam(vecPrevious,
                                vecNew,
                                5, 2, true);

                vecPrevious = vecNew;
            }

            Voxels voxLat = new(lat);

            Lattice latSphere = new();
            latSphere.AddSphere(new(50, 50, 50), 40);
            Voxels voxSphere = new(latSphere);

            voxLat.BoolIntersect(voxSphere);

            Library.oViewer().Add(voxLat);

        }
    }
}