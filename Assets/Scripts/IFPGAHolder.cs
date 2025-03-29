using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.Objects.Pipes;
using UnityEngine;

namespace fpgamod
{
  public interface IFPGAHolder : ILogicable
  {
    public BasicFPGAChip GetFPGAChip();
  }
}
