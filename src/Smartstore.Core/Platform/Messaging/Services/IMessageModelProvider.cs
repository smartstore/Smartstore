using Smartstore.Collections;
using Smartstore.Templating;

namespace Smartstore.Core.Messaging
{
    /// <summary>
    /// Responsible for building the message template model.
    /// </summary>
    public interface IMessageModelProvider
    {
        /// <summary>
        /// Creates and adds all global model parts to the template model (<seealso cref="MessageContext.Model"/>):
        ///	<para>
        ///		<list type="bullet">
        ///			<item>Context (contains meta infos like template name, language etc.)</item>
        ///			<item>Customer (obtained from <paramref name="messageContext"/> <see cref="MessageContext.Customer"/> property)</item>
        ///			<item>Store (obtained from <paramref name="messageContext"/> <see cref="MessageContext.Store"/> property)</item>
        ///			<item>Email (the <see cref="EmailAccount"/>)</item>
        ///			<item>Theme (some theming variables, mostly colors)</item>
        ///		</list>
        ///	</para>
        /// </summary>
        /// <param name="messageContext">Contains all data required for building a model part and creating a message</param>
        Task AddGlobalModelPartsAsync(MessageContext messageContext);

        /// <summary>
        /// Adds a template specific model part to the template model.
        ///	The passed object instance (<paramref name="part"/>) will be converted to a special type which the underlying <see cref="ITemplateEngine"/> can handle.
        /// <para>
        ///		Supported types are: Order, Product, Address, Shipment, OrderNote, 
        ///		RecurringPayment, ReturnRequest, GiftCard, <see cref="NewsletterSubscription"/>, <see cref="Campaign"/>, 
        ///		ProductReview, BlogComment, NewsComment, ForumTopic, ForumPost, Forum, PrivateMessage.
        /// </para>
        /// <para>
        ///		Furthermore, any object implementing <see cref="IModelPart"/> or <see cref="INamedModelPart"/> can also be passed as model part.
        ///		The first merges all entries within the passed object with the special <c>Bag</c> entry, the latter creates a whole
        ///		new entry using the name provided by its <see cref="INamedModelPart.ModelPartName"/> property.
        /// </para>
        /// <para>
        ///		If an unsupported object is passed, the framework will publish the <see cref="Smartstore.Core.Messaging.Events.MessageModelPartMappingEvent"/> event, giving
        ///		a subscriber the chance to provide a converted model object and a part name.
        /// </para>
        /// </summary>
        /// <param name="part">The model part instance to convert and add to the final model.</param>
        /// <param name="messageContext">Contains all data required for building a model part and creating a message</param>
        /// <param name="name">
        /// The name to use for the model part in the final model. If <c>null</c>, the framework tries to infer the name.
        /// See also <see cref="ResolveModelName(object)"/>
        /// </param>
        Task AddModelPartAsync(object part, MessageContext messageContext, string name = null);

        /// <summary>
        /// Creates a serializable model object for the passed entity/object.
        /// <para>
        ///		Supported types are: Order, Product, Address, Shipment, OrderNote, 
        ///		RecurringPayment, ReturnRequest, GiftCard, <see cref="NewsletterSubscription"/>, <see cref="Campaign"/>, 
        ///		ProductReview, BlogComment, NewsComment, ForumTopic, ForumPost, Forum, PrivateMessage.
        /// </para>
        /// <para>
        ///		Furthermore, any object implementing <see cref="IModelPart"/> or <see cref="INamedModelPart"/> can also be passed as model part.
        ///		The first merges all entries within the passed object with the special <c>Bag</c> entry, the latter creates a whole
        ///		new entry using the name provided by its <see cref="INamedModelPart.ModelPartName"/> property.
        /// </para>
        /// <para>
        ///		If an unsupported object is passed, <c>null</c> is returned
        /// </para>
        /// </summary>
        /// <param name="part">The model part instance to convert.</param>
        /// <param name="ignoreNullMembers">Whether members/properties with null values should be excluded from the result model.</param>
        /// <param name="ignoreMemberNames">Optional list of member/property names to exclude from the result model.</param>
        Task<object> CreateModelPartAsync(object part, bool ignoreNullMembers, params string[] ignoreMemberNames);

        /// <summary>
        /// Tries to infer the model part name by type:
        /// <list type="bullet">
        ///		<item>When <paramref name="model"/> is a plain object: type name</item>
        ///		<item>When <paramref name="model"/> is <see cref="INamedModelPart"/>: <c>ModelPartName</c> property</item>
        /// </list>
        /// </summary>
        /// <param name="model">The model part instance to resolve a name for.</param>
        /// <returns>The inferred name or <c>null</c></returns>
        string ResolveModelName(object model);

        /// <summary>
        /// Build a model metadata tree for a final template model. Model trees are used
        /// on the client to provide autocomplete information.
        /// </summary>
        /// <param name="model">The final template model to build a model tree for.</param>
        /// <returns>A hierarchy of <see cref="ModelTreeMember"/> instances.</returns>
        TreeNode<ModelTreeMember> BuildModelTree(TemplateModel model);

        /// <summary>
        /// Gets the last known model metadata tree for a particular template.
        /// See also <see cref="BuildModelTree(TemplateModel)"/>
        /// </summary>
        /// <param name="messageTemplateName">Name of the template to get metadata for.</param>
        /// <returns>A hierarchy of <see cref="ModelTreeMember"/> instances.</returns>
        Task<TreeNode<ModelTreeMember>> GetLastModelTreeAsync(string messageTemplateName);

        /// <summary>
        /// Gets the last known model metadata tree for a particular template.
        /// See also <see cref="BuildModelTree(TemplateModel)"/>
        /// </summary>
        /// <param name="template">The template to get metadata for.</param>
        /// <returns>A hierarchy of <see cref="ModelTreeMember"/> instances.</returns>
        TreeNode<ModelTreeMember> GetLastModelTree(MessageTemplate template);
    }
}
