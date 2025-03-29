using System.Collections;
using System.Collections.Generic;
using Assets.Scripts;
using UnityEngine;

namespace fpgamod
{
  public interface ILocalizedPrefab
  {
    Localization.LocalizationThingDat GetLocalization();
  }
}
