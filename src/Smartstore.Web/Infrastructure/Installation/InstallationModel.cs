using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Infrastructure.Installation
{
    public partial class InstallationModel : ModelBase
    {
        [Required, DataType(DataType.EmailAddress)]
        public string AdminEmail { get; set; }

        [Required, DataType(DataType.Password)]
        public string AdminPassword { get; set; }

        [Required, DataType(DataType.Password), Compare(nameof(AdminPassword))]
        public string ConfirmPassword { get; set; }

        [Required]
        public string DataProvider { get; set; } = "sqlserver";

        public string DatabaseConnectionString { get; set; }

        [Required]
        public string PrimaryLanguage { get; set; }

        public string MediaStorage { get; set; }

        public bool CreateDatabase { get; set; } = true;

        public bool InstallSampleData { get; set; }

        #region SqlServer

        public string SqlConnectionInfo { get; set; } = "sqlconnectioninfo_values";

        public string SqlServerName { get; set; }

        public string SqlDatabaseName { get; set; }

        public string SqlServerUsername { get; set; }

        [DataType(DataType.Password)]
        public string SqlServerPassword { get; set; }

        public string SqlAuthenticationType { get; set; } = "sqlauthentication";

        public bool UseCustomCollation { get; set; }

        public string Collation { get; set; } = "SQL_Latin1_General_CP1_CI_AS";

        #endregion

        #region MySql

        // ...

        #endregion
    }
}
