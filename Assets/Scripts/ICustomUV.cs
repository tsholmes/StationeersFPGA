using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace fpgamod
{
    public interface ICustomUV
    {
      Vector2? GetUV(GameObject obj);
    }
}
