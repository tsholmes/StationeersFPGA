using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fpgamod
{
  public static class FPGAExamples
  {
    public static readonly FPGAExample[] Examples = new FPGAExample[]{
      new() {
        Title="Gas Calculator",
        Tooltip="Calculates components of the ideal gas law",
        RawConfig= ""+
          "# calculates each component of the ideal gas law (PV=nRT) from the other components\n" +
          "# constants\n" +
          "lut00=R 8.3144\n" +
          "# inputs\n" +
          "in00=PressureIn\n" +
          "in01=VolumeIn\n" +
          "in02=TotalMolesIn\n" +
          "in03=TemperatureIn\n" +
          "# outputs\n" +
          "gate00=Pressure / nRT VolumeIn\n" +
          "gate01=Volume / nRT PressureIn\n" +
          "gate02=TotalMoles / PV RT\n" +
          "gate03=Temperature / PV nR\n" +
          "# intermediate values\n" +
          "gate40=PV * PressureIn VolumeIn\n" +
          "gate41=nR * TotalMolesIn R\n" +
          "gate42=RT * R TemperatureIn\n" +
          "gate43=nRT * nR TemperatureIn\n"
      },
    };
  }

  public struct FPGAExample
  {
    public string Title;
    public string Tooltip;
    public string RawConfig;
  }
}
