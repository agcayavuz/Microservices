namespace BasketService.Application.Options
{
    public sealed class BasketOptions
    {
        public bool AutoDeleteEmptyOnItemRemove { get; set; } = false;
        public enumCreateOrReplaceMode CreateOrReplaceBehavior { get; set; } = enumCreateOrReplaceMode.Merge;
    }
}
