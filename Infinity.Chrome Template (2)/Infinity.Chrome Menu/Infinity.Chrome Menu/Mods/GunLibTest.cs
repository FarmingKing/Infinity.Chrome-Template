using Kings.Classes;
using UnityEngine;
using static Kings.Classes.GunLib;

namespace GunLibTest
{
    public class GunLibTest1
    {
        // how to use the gunlib
        public static void TestGunLib()
        {
            GunLib.StartBothGuns(() =>
            {
                
            }, false);
        }

        public static void TestGunLibLOCK()
        {
            GunLib.StartBothGuns(() =>
            {
                
            }, true);
        }
    }
}