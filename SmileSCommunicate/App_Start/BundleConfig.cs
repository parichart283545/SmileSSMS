using System.Web.Optimization;
using WebHelpers.Mvc5;

namespace SmileSCommunicate.App_Start
{
    public class BundleConfig
    {
        public static void RegisterBundles(BundleCollection bundles)
        {
            bundles.Add(new StyleBundle("~/Bundles/css")
                .Include("~/Content/css/bootstrap.min.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/css/bootstrap-select.css")
               .Include("~/Content/css/bootstrap-datepicker3.min.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/js/plugins/datepicker/css/datepicker.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/css/font-awesome.min.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/css/icheck/blue.min.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/js/plugins/datatable/datatables.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/js/plugins/select2/dist/css/select2.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/js/plugins/sweetalert/dist/sweetalert.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/css/AdminLTE.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/js/plugins/bootstrap-timepicker/package/css/bootstrap-timepicker.min.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/css/style.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/js/plugins/toastr/toastr.min.css", new CssRewriteUrlTransformAbsolute())
               .Include("~/Content/css/skins/skin-blue.css")
               .Include("~/Content/js/plugins/timepicker/jquery.timepicker.min.css", new CssRewriteUrlTransformAbsolute())
                .Include("~/Content/js/plugins/jquery-confirm/jquery-confirm.min.css"));

            bundles.Add(new ScriptBundle("~/Bundles/js")
                .Include("~/Content/js/plugins/jquery/jquery-2.2.4.js")
                .Include("~/Content/js/plugins/bootstrap/bootstrap.js")
                .Include("~/Content/js/plugins/fastclick/fastclick.js")
                .Include("~/Content/js/plugins/slimscroll/jquery.slimscroll.js")
                .Include("~/Content/js/plugins/bootstrap-select/bootstrap-select.js")
                .Include("~/Content/js/plugins/moment/moment.js")
                .Include("~/Content/js/plugins/datepicker/js/bootstrap-datepicker.js")
                .Include("~/Content/js/plugins/datepicker/js/bootstrap-datepicker-thai.js")
                .Include("~/Content/js/plugins/datepicker/js/locales/bootstrap-datepicker.th.js")
                .Include("~/Content/js/plugins/icheck/icheck.js")
                .Include("~/Content/js/plugins/validator.js")
                .Include("~/Content/js/plugins/inputmask/jquery.inputmask.bundle.js")
                .Include("~/Content/js/plugins/datatable/datatables.js")
                .Include("~/Content/js/plugins/numeral-js/min/numeral.js")
                .Include("~/Content/js/plugins/jquery-validation/dist/jquery.validate.js")
                .Include("~/Content/js/plugins/select2/dist/js/select2.js")
                .Include("~/Content/js/plugins/sweetalert/dist/sweetalert.min.js")
                .Include("~/Content/js/plugins/chart/chart.min.js")
                .Include("~/Content/js/plugins/bootstrap-timepicker/package/js/bootstrap-timepicker.min.js")
                .Include("~/Content/js/plugins/jquery-loading-overlay/dist/loadingoverlay.min.js")
                .Include("~/Content/js/adminlte.js")
                .Include("~/Content/js/init.js")
                .Include("~/Content/js/jqValidatorMessage.js")
                .Include("~/Scripts/jquery.signalR-2.2.3.min.js")
                .Include("~/Content/js/plugins/toastr/toastr.min.js")
                .Include("~/Content/js/waitingDialog.js")
                .Include("~/Content/js/plugins/timepicker/jquery.timepicker.min.js")
                .Include("~/Content/js/plugins/jquery-confirm/jquery-confirm.min.js"));

#if DEBUG
            BundleTable.EnableOptimizations = false;
#else
            BundleTable.EnableOptimizations = true;
#endif
        }
    }
}