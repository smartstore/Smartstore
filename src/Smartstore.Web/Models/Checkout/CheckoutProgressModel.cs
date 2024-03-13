namespace Smartstore.Web.Models.Checkout
{
    public partial class CheckoutProgressStepModel : ModelBase
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Url { get; set; }
        public bool Visited { get; set; }
        public bool Active { get; set; }

        public string StateClass
        {
            get
            {
                if (Visited)
                {
                    return "visited";
                }
                if (Active)
                {
                    return "active";
                }

                return "inactive";
            }
        }
    }
}