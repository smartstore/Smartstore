using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Smartstore.Core.Rules
{
    [Table("Rule")]
    [Index(nameof(RuleType), Name = "IX_PageBuilder_RuleType")]
    [Index(nameof(DisplayOrder), Name = "IX_PageBuilder_DisplayOrder")]
    public partial class RuleEntity : BaseEntity, ICloneable<RuleEntity>
    {
        [Required]
        public int RuleSetId { get; set; }

        private RuleSetEntity _ruleSet;
        [ForeignKey("RuleSetId")]
        [IgnoreDataMember]
        public RuleSetEntity RuleSet
        {
            get => _ruleSet = LazyLoader.Load(this, ref _ruleSet);
            set => _ruleSet = value;
        }

        [Required, StringLength(100)]
        public string RuleType { get; set; }

        [Required, StringLength(20)]
        public string Operator { get; set; }

        [MaxLength]
        public string Value { get; set; }

        public int DisplayOrder { get; set; }

        [NotMapped]
        public bool IsGroup => RuleType.EqualsNoCase("Group");

        public RuleEntity Clone()
        {
            return new()
            {
                RuleSetId = RuleSetId,
                RuleType = RuleType,
                Operator = Operator,
                Value = Value,
                DisplayOrder = DisplayOrder
            };
        }

        object ICloneable.Clone()
            => Clone();
    }
}