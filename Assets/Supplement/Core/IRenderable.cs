namespace Supplement.Core
{
    public interface IRenderable
    {
    }

    public interface IRenderable<T> : IRenderable
    {
        void Render(T dto);
    }
}