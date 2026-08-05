namespace FusionCanvas.Application.Items.Import;

public interface IItemCsvCodec
{
    ItemCsvParseResult Parse(string source);

    string WriteSample();
}
