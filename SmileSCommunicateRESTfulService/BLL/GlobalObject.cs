using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using SmileSCommunicateRESTfulService.EmployeeService;
using SmileSCommunicateRESTfulService.Models;
using SmileSCommunicateRESTfulService.Properties;
//using SmileSMobileAppToken;

namespace SmileSCommunicateRESTfulService.BLL
{
    public static class GlobalObject
    {
        public static JsonSerializerSettings carmelSetting()
        {
            return new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        }

        //public static Model.StdResult CheckToken(ApiController c)
        //{
        //    var result = new Model.StdResult();
        //    try
        //    {
        //        // Get value from webconfig
        //        bool publishUAT = Settings.Default.PublishUAT;

        //        // Check ว่าเป็น uat หรือ production
        //        if(publishUAT == true)
        //        {
        //            //Validate UAT Token
        //            if(!new AlignToken().IsValidToken(c))
        //            {
        //                result.Result = Model.MappingCode.ResultMessage.Failure.ToString();
        //                result.MSG = "Invalid Token";
        //            }
        //        }
        //        else
        //        {
        //            //Validate Align Token
        //            if(!new AlignToken().IsValidToken(c))
        //            {
        //                result.Result = Model.MappingCode.ResultMessage.Failure.ToString();
        //                result.MSG = "Invalid Token";
        //            }
        //        }
        //    }
        //    catch
        //    {
        //        result.Result = Model.MappingCode.ResultMessage.Failure.ToString();
        //        result.MSG = "Need Token";
        //    }

        //    return result;
        //}

        public static int GetUserID(string empCode)
        {
            var employeeClient = new EmployeeServiceClient();
            int userID = employeeClient.GetUserIDByEmpCode(empCode);

            return userID;
        }

        public static int GetEmployeeID(string empCode)
        {
            var employeeClient = new EmployeeServiceClient();
            int employeeId = employeeClient.GetEmployeeByEmployeeCode(empCode).Employee_ID;

            return employeeId;
        }

        public static int GetTeamID(string empCode)
        {
            var employeeClient = new EmployeeServiceClient();
            int employeeId = employeeClient.GetEmployeeByEmployeeCode(empCode).EmployeeTeam_ID;

            return employeeId;
        }

        public static int GetBranchID(string empCode)
        {
            var employeeClient = new EmployeeServiceClient();
            int employeeId = employeeClient.GetEmployeeByEmployeeCode(empCode).Branch_ID;

            return employeeId;
        }
    }
}