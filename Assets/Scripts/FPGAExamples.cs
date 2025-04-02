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
        RawConfig=
@"# calculates each component of the ideal gas law (PV=nRT) from the other components
# constants
lut00=R 8.3144
# inputs
in00=PressureIn
in01=VolumeIn
in02=TotalMolesIn
in03=TemperatureIn
# outputs
gate00=Pressure / nRT VolumeIn
gate01=Volume / nRT PressureIn
gate02=TotalMoles / PV RT
gate03=Temperature / PV nR
# intermediate values
gate40=PV * PressureIn VolumeIn
gate41=nR * TotalMolesIn R
gate42=RT * R TemperatureIn
gate43=nRT * nR TemperatureIn
"
      },
      new() {
        Title="Raw Config Formatting",
        Tooltip="Contains examples of how to write raw configurations",
        RawConfig=
@"# anything after the # character on each line is a comment and is ignored
# lines containing only a comment (or nothing) are ignored

# configuration lines contain an address, an optional label, and the required values for the configuration type
# [address] [configs...]
# or
# [address]=label [configs...]
# labels can contain any non-whitespace characters other than #

# input configurations only contain an address and label
in00=Input0Label
 
# addresses have 3 valid formats:
# name: [section]00-[section]63
#   in00-in63, gate00-gate63, lut00-lut63
# decimal: 0-191
#   inputs 0-63, gates 64-127, lookup 128-191
# hexadecimal: $00-$BF
#   inputs $00-$3F, gates $40-7F, lookup $80-BF
1=Input1Decimal
$02=Input2Hex

# lookup table configurations contain an address, an optional label, and a value
lut00=Lookup0 100
129=Lookup1 200.000
$82=Lookup2 3e2

# gate configurations contain an address, an optional label, and an op configuration
# standard op configurations contain an op symbol and any required inputs
# the op symbols and number of inputs can be found on the help tab
# inputs are specified by name, hexadecimal address, or label (not decimal).
gate00=in0+lut0 + in00 lut00
65=in1*lut1 * $01 $81
$42=abs(in2) abs Input2Hex
# raw op configurations contain a single hexadecimal value describing the gate
# the lowest byte is the op code, the next 2 bytes are the addresses of the input values
# $[v1address][v0address][opcode]
# raw op codes can be found on the help tab
gate03=raw(in1/lut2) $820114 # op(/) = $14, in1=$01 lut2=$82

# warnings
# any extra values on a line are ignored
in03=Warn extra values are ignored
# duplicate configurations for an address are ignored
in03=WarnDuplicate
"
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
