namespace fpgamod
{
  public interface IPatchOnLoad
  {
    void PatchOnLoad();

    bool SkipMaterialPatch() => false;
  }
}