namespace LibCore
{
	public class CouponGameDetails
	{
		public string Title
		{
			get;
			set;
		}

		public string Client
		{
			get;
			set;
		}

		public string Location
		{
			get;
			set;
		}

		public string Venue
		{
			get;
			set;
		}

		public string Country
		{
			get;
			set;
		}

		public string GeoRegion
		{
			get;
			set;
		}

		public string Notes
		{
			get;
			set;
		}

		public int PlayerCount
		{
			get;
			set;
		}

		public string Purpose
		{
			get;
			set;
		}

		public string ClassId
		{
			get;
			set;
		}

		public string RossCode
		{
			get;
			set;
		}

		public string Type
		{
			get;
			set;
		}

		public string Content
		{
			get;
			set;
		}

		public string InvoiceAddress
		{
			get;
			set;
		}

		public string PoReference
		{
			get;
			set;
		}

		public string ChargeCompany
		{
			get;
			set;
		}

		public string Sponser
		{
			get;
			set;
		}

		public string DeclaredParentCompany
		{
			get;
			set;
		}

		// is this different to sponsor??
		public string EventSponsor
		{
			get;
			set;
		}

		public string Billing
		{
			get;
			set;
		}

		public string CostCentre
		{
			get;
			set;
		}

		public string PartnerName
		{
			get;
			set;
		}

		public string PartnerNumber
		{
			get;
			set;
		}

		public CouponGameDetails()
		{
			// set some defaults in case nulls cause problems in existing code
			Title = string.Empty;
			Client = string.Empty;
			Location = string.Empty;
			Venue = string.Empty;
			Country = string.Empty;
			GeoRegion = string.Empty;
			Notes = string.Empty;
			PlayerCount = 0;
			Purpose = string.Empty;
			ClassId = string.Empty;
			RossCode = string.Empty;
			Type = string.Empty;
			Content = string.Empty;
			InvoiceAddress = string.Empty;
			PoReference = string.Empty;
			ChargeCompany = string.Empty;
			Sponser = string.Empty;
			DeclaredParentCompany = string.Empty;
			EventSponsor = string.Empty;
			Billing = string.Empty;
			CostCentre = string.Empty;
			PartnerName = string.Empty;
			PartnerNumber = string.Empty;
		}
	}
}