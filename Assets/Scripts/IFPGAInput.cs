namespace fpgamod
{
  public interface IFPGAInput
  {
    double GetFPGAInputPin(int index);
    long GetFPGAInputModCount();
  }
}