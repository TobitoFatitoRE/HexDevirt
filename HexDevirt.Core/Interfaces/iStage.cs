namespace HexDevirt.Core
{
    public interface iStage
    {
        public string Name { get; }
        public string Description { get; }
        public void Execute(DevirtualizationCtx ctx);
    }
}