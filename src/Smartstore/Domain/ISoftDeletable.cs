using System;
using System.Collections.Generic;

namespace Smartstore.Domain
{
	public interface ISoftDeletable
	{
		bool Deleted { get; }
	}
}