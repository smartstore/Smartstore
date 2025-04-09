namespace Smartstore.Core.Identity
{
    public class CustomerDeletionResult
    {
        public CustomerDeletionResult(
            int[] deletedGuestCustomerIds = null, 
            int[] softDeletedCustomerIds = null, 
            int[] skippedAdminIds = null)
        {
            DeletedGuestsIds = deletedGuestCustomerIds ?? [];
            SoftDeletedCustomersIds = softDeletedCustomerIds ?? [];
            SkippedAdminsIds = skippedAdminIds ?? [];
        }

        /// <summary>
        /// Gets the IDs of physically deleted guest customers.
        /// </summary>
        public int[] DeletedGuestsIds { get; }

        /// <summary>
        /// Gets the IDs of soft-deleted customers.
        /// </summary>
        public int[] SoftDeletedCustomersIds { get; }

        /// <summary>
        /// Gets the IDs of customers that were skipped because they are administrators.
        /// </summary>
        public int[] SkippedAdminsIds { get; }

        /// <summary>
        /// Gets the total number of deleted customers.
        /// </summary>
        public int NumDeleted
            => DeletedGuestsIds.Length + SoftDeletedCustomersIds.Length;

        /// <summary>
        /// Gets the IDs of all deleted customers, including both physically deleted guests and soft-deleted customers.
        /// </summary>
        public int[] AllDeletedCustomersIds
            => [.. DeletedGuestsIds.Concat(SoftDeletedCustomersIds).OrderBy(x => x)];

        public override string ToString()
        {
            return "numDeleted:{0} deletedGuests:{1} softDeletedCustomers:{2} skippedAdmins:{3}".FormatInvariant(
                NumDeleted,
                string.Join(',', DeletedGuestsIds),
                string.Join(',', SoftDeletedCustomersIds),
                string.Join(',', SkippedAdminsIds));
        }
    }
}
