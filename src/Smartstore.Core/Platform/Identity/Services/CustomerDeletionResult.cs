namespace Smartstore.Core.Identity
{
    public class CustomerDeletionResult
    {
        public CustomerDeletionResult(
            int[] deletedGuestCustomerIds = null, 
            int[] softDeletedCustomerIds = null, 
            int[] skippedAdminIds = null)
        {
            DeletedGuestIds = deletedGuestCustomerIds ?? [];
            SoftDeletedCustomerIds = softDeletedCustomerIds ?? [];
            SkippedAdminIds = skippedAdminIds ?? [];
        }

        /// <summary>
        /// Gets the IDs of physically deleted guest customers.
        /// </summary>
        public int[] DeletedGuestIds { get; }

        /// <summary>
        /// Gets the IDs of soft-deleted customers.
        /// </summary>
        public int[] SoftDeletedCustomerIds { get; }

        /// <summary>
        /// Gets the IDs of customers that were skipped because they are administrators.
        /// </summary>
        public int[] SkippedAdminIds { get; }

        /// <summary>
        /// Gets the total number of deleted customers.
        /// </summary>
        public int NumDeleted
            => DeletedGuestIds.Length + SoftDeletedCustomerIds.Length;

        /// <summary>
        /// Gets the IDs of all deleted customers, including both physically deleted guests and soft-deleted customers.
        /// </summary>
        public int[] AllDeletedCustomerIds
            => [.. DeletedGuestIds.Concat(SoftDeletedCustomerIds).OrderBy(x => x)];

        public override string ToString()
        {
            return "numDeleted:{0} deletedGuests:{1} softDeletedCustomers:{2} skippedAdmins:{3}".FormatInvariant(
                NumDeleted,
                string.Join(',', DeletedGuestIds),
                string.Join(',', SoftDeletedCustomerIds),
                string.Join(',', SkippedAdminIds));
        }
    }
}
