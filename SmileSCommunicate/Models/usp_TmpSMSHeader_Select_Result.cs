//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace SmileSCommunicate.Models
{
    using System;
    
    public partial class usp_TmpSMSHeader_Select_Result
    {
        public int TmpSMSHeaderId { get; set; }
        public string TmpSMSHeaderCode { get; set; }
        public Nullable<System.DateTime> Period { get; set; }
        public Nullable<int> SMSTypeId { get; set; }
        public string SMSType { get; set; }
        public Nullable<int> PaymethodTypeId { get; set; }
        public string PaymethodType { get; set; }
        public string Remark { get; set; }
        public Nullable<int> TmpSMSHeaderStatusId { get; set; }
        public string TmpSMSHeaderStatus { get; set; }
        public Nullable<int> Total { get; set; }
        public Nullable<bool> IsActive { get; set; }
        public Nullable<System.DateTime> CreatedDate { get; set; }
        public Nullable<int> CreatedByUserId { get; set; }
        public string CreatedByCode { get; set; }
        public string CreatedByName { get; set; }
        public Nullable<System.DateTime> UpdatedDate { get; set; }
        public Nullable<int> UpdatedByUserId { get; set; }
        public Nullable<bool> IsGenerateLink { get; set; }
        public Nullable<bool> IsKeepDetail { get; set; }
        public Nullable<int> TotalCount { get; set; }
    }
}
