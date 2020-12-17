using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using Smartstore.Domain;

namespace Smartstore.Core.Rules
{
    [Table("Rule")]
    [Index(nameof(RuleType), Name = "IX_PageBuilder_RuleType")]
    [Index(nameof(DisplayOrder), Name = "IX_PageBuilder_DisplayOrder")]
    public partial class RuleEntity : BaseEntity
    {
        private readonly ILazyLoader _lazyLoader;

        public RuleEntity()
        {
        }

        public RuleEntity(ILazyLoader lazyLoader)
        {
            _lazyLoader = lazyLoader;
        }

        [Required]
        public int RuleSetId { get; set; }

        private RuleSetEntity _ruleSet;
        [ForeignKey("RuleSetId")]
        public RuleSetEntity RuleSet
        {
            get => _lazyLoader?.Load(this, ref _ruleSet) ?? _ruleSet;
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
    }
}