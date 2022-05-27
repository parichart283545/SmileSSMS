$(window).on('load', function () {
    $("#loading").fadeOut("slow");
});
$(function () {
    //$('form').validate({
    //    rules:
    //    {
    //        sendType: { required: true },
    //    },
    //    messages:
    //    {
    //        sendType: { required: "<br>กรุณาเลือก..<br/>" },
    //    },
    //    errorPlacement: function (error, element) {
    //        if (element.is(":radio")) {
    //            error.prependTo(element.parents('.rdSendType'));
    //        } else { // This is the default behavior
    //            error.insertAfter(element);
    //        }
    //    }
    //});

    $(".datepicker").datepicker({
        autoclose: true
    });

    $('input[type="checkbox"], input[type="radio"]').iCheck({
        checkboxClass: "icheckbox_minimal-blue",
        radioClass: "iradio_minimal-blue"
    });

    $("#datemask").inputmask("dd/mm/yyyy", { "placeholder": "dd/mm/yyyy" });
    $("#datemask2").inputmask("mm/dd/yyyy", { "placeholder": "mm/dd/yyyy" });
    $("[data-mask]").inputmask();

    //add method validator check N/A
    jQuery.validator.addMethod("checkNA", function (value, element) {
        if (value == "-1") {
            return false;
        } else {
            return true;
        };
    }, "กรุณาเลือก");

    //jquery validation check identity ID Card
    jQuery.validator.addMethod("checkID", function (value, element) {
        $(element).parent().removeClass('form-group has-error');
        $(element).parent().removeClass('form-group has-success');
        if (value == '') {
            return true;
        }
        if (value.length != 13) {
            $(element).parent().addClass('form-group has-error');
            return false;
        }
        var sum = 0;
        for (var i = 0; i < 12; i++) {
            sum += parseFloat(value.charAt(i)) * (13 - i);
        }
        if ((11 - (sum % 11)) % 10 != parseFloat(value.charAt(12))) {
            $(element).parent().addClass('form-group has-error');
            return false;
        }
        $(element).parent().addClass('form-group has-success');
        return true;
    }, "เลขบัตรประจำตัวประชาชนไม่ถูกต้อง!");
});

//Display date from json (Buddhist era)
function DisplayJsonDateBE(data) {
    var result = "";
    if (data != null) {
        var d = moment(data).toDate();
        var month = ("0" + (d.getMonth() + 1)).slice(-2);
        var day = ("0" + d.getDate()).slice(-2);
        var year = d.getFullYear() + 543;
        result = day + '/' + month + '/' + year;
    }
    return result;
};

const swal_success = (success, cancel) => {
    window.swal({
        title: 'ดำเนินการเรียบร้อย!',
        type: 'success',
        showCancelButton: false,
        showConfirmButton: true,
        closeOnEsc: false
    }, (IsConfirm) => {
        if (IsConfirm) {
            if (typeof success === "function") {
                //callback
                success();
            }
        } else {
            if (typeof cancel === "function") {
                //callback
                cancel();
            }
        }
    });
};

const swal_fail = (text, callback) => {
    window.swal({
        title: 'เกิดข้อผิดพลาด!',
        type: 'error',
        text: text,
        showCancelButton: false,
        confirmButtonColor: '#ed2b09',
        confirmButtonText: 'ตกลง',
        closeOnEsc: false
    }, (IsConfirm) => {
        if (IsConfirm) {
            if (typeof callback === "function") {
                //callback
                callback();
            }
        }
    });
};
const swal_confirm = (title, text, success, cancel) => {
    window.swal({
        title: title,
        text: text,
        type: 'info',
        showCancelButton: true,
        confirmButtonColor: '#277020',
        confirmButtonText: 'ยืนยัน',
        cancelButtonText: 'ยกเลิก',
        closeOnConfirm: false,
        closeOnEsc: false,
        closeOnCancel: true
    }, (IsConfirm) => {
        if (IsConfirm) {
            if (typeof success === "function") {
                //callback
                success();
            }
        } else {
            if (typeof cancel === "function") {
                //callback
                cancel();
            }
        }
    });
};

const loadingSpinner = (fadeType, delay = 999999) => {
    switch (fadeType) {
        case "fadeIn":
            $("#loading").fadeIn(500, (e) => {
                setTimeout(() => {
                    $("#loading").fadeOut();
                }, delay);
            });
            break;
        case "fadeOut":
            $("#loading").fadeOut(2000);
            break;
        default:
    }
}

/********/
function formatNumber(num) {
    return num.toString().replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,');
}