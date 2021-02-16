using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Smartstore.Web.Modelling;

namespace Smartstore.Web.Models
{
    // TODO: (mh) (core) Remove TestModel later
    public class TestModelMH : ModelBase
    {
        [LocalizedDisplay("*CampaingCount")]
        public int CampaingCount { get; set; }

    }
}
