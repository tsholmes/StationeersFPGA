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
      new() {
        Title="Evaporation Calculator",
        Tooltip="Calculates evaporation pressure and temperature",
        RawConfig=
@"#inputs
in00=GasIndex
in01=Pressure
in02=Temperature

#outputs
gate00=Hash + hashBase hashOff # shows gas icon on hash display in gas mode
gate01=EvapTemp + tout v0 # evaporation temperature at input pressure
gate02=EvapPressure + pout v0 # evaporation pressure at input temperature

# intermediate values
# clamp index to valid range
gate08=idxClamp max GasIndex v0
gate09=idx min idxClamp maxIndex
# flags for specific indexes
gate11=isCO2 == idx idx_CO2
gate12=isPol == idx idx_Pol
gate13=isNos == idx idx_Nos
gate14=isH == idx idx_H
gate15=isPWater == idx idx_PWater
# calculate hash
gate16=hashBase pow v2 idx
gate17=hashOff1 * isH H_Hash_off
gate18=hashOff2 * isPWater PWater_Hash_off
gate19=hashOff + hashOff1 hashOff2
# lookup coefficients
gate21=A @ idx
gate22=idxB + idx gasCount
gate23=B @ idxB
# lookup temp range
gate24=idxTMin + idxB gasCount
gate25=TMin @ idxTMin
gate26=idxTMax + idxTMin gasCount
gate27=TMax @ idxTMax
# calc min pressure
gate32=pminOff1 * isCO2 CO2_PMin_off
gate33=pminOff2 * isPol Pol_PMin_off
gate34=pminOff3 * isNos Nos_PMin_off
gate35=pminOff4 + pminOff1 pminOff2
gate36=pminOff + pminOff3 pminOff4
gate37=PMin + PMin_base pminOff
# calc max pressure
gate38=pmaxOff * isNos Nos_PMax_off
gate39=PMax + PMax_base pmaxOff

# clamp input pressure
gate48=pinClamp max PMin Pressure
gate49=pin min Pressure PMax
# calculate evap temp
gate50=tout_base / pin A
gate51=tout_exp / v1 B
gate52=tout_raw pow tout_base tout_exp
# clamp evap temp
gate53=toutClamp max TMin tout_raw
gate55=tout min toutClamp TMax

# clamp input temp
gate56=tinClamp max TMin Temperature
gate57=tin min tinClamp TMax
# calculate evap pressure
gate58=pout_pow pow tin B
gate59=pout_raw * A pout_pow
# clamp evap pressure
gate60=poutClamp max PMin pout_raw
gate63=pout min poutClamp PMax

# evap coefficient A
lut00=Ox_A 2.6854996004E-11
lut01=N_A 5.5757107833E-07
lut02=CO2_A 1.579573E-26
lut03=Vol_A 5.863496734E-15
lut04=Pol_A 2.079033884
lut05=Water_A 3.8782059839E-19
lut06=Nos_A 0.065353501531
lut07=H_A 3.18041E-05
lut08=PWater_A 4E-20

# evap coefficient B
lut09=Ox_B 6.49214937325
lut10=N_B 4.40221368946
lut11=CO2_B 12.195837931
lut12=Vol_B 7.8643601035
lut13=Pol_B 1.31202194555
lut14=Water_B 7.90030107708
lut15=Nos_B 1.70297431874
lut16=H_B 4.4843872973
lut17=PWater_B 8.27025711260823

# min liquid temp
lut18=Ox_TMin 56.416
lut19=N_TMin 40.01
lut20=CO2_TMin 217.82
lut21=Vol_TMin 81.6
lut22=Pol_TMin 173.32
lut23=Water_TMin 273.15
lut24=Nos_TMin 252.1
lut25=H_TMin 16
lut26=PWater_TMin 276.15

# max liquid temp
lut27=Ox_TMax 162.2
lut28=N_TMax 190
lut29=CO2_TMax 265
lut30=Vol_TMax 195
lut31=Pol_TMax 425
lut32=Water_TMax 643
lut33=Nos_TMax 430.6
lut34=H_TMax 70
lut35=PWater_TMax 629

# liquid pressure range
lut36=PMin_base 6.3 # most common min pressure
lut37=PMax_base 6000 # most common max pressure

# indexes of specific gases
lut40=idx_CO2 2
lut41=idx_Pol 4
lut42=idx_Nos 6
lut43=idx_H 7
lut44=idx_PWater 8

# parameter offsets
lut48=CO2_PMin_off 510.7 # 517
lut49=Pol_PMin_off 1793.7 # 1800
lut50=Nos_PMin_off 793.7 # 800
lut51=Nos_PMax_off -4000 # 2000
lut52=H_Hash_off 16256 # 128 -> 16384
lut53=PWater_Hash_off 65280 # 256 -> 65536

# helper constants
lut56=v0 0
lut57=v1 1
lut58=v2 2
lut59=gasCount 9 # number of gas types
lut60=maxIndex 8
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
