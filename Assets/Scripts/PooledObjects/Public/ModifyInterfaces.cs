using Anime;

public interface IVertical
{
    Pack VerticalCache { get; }

    float Vertical { get; set; }

    void UpdateCache();
}
