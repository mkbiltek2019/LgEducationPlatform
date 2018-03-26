﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Lg.EducationPlatform.Common;
using Lg.EducationPlatform.Model;
using Lg.EducationPlatform.IBLL;
using Lg.EducationPlatform.WebHelper;
using Lg.EducationPlatform.jqDataTableModel;
using Lg.EducationPlatform.ViewModel;
using System.Linq.Expressions;

namespace Lg.EducationPlatform.Web.Controllers
{
    public class StudentController : BaseController
    {
        private IUsersService _userService = OperateHelper.Current._serviceSession.UsersService;
        private IStudentsService _studentsService = OperateHelper.Current._serviceSession.StudentsService;
        // GET: Student
        public ActionResult Index()
        {
            UserDto user = ViewBag.User as UserDto;
            if (user.RoleId == 0)
            {
                var list = _userService.GetTeacherItems();
                list.Insert(0, new ItemModel { Text = "全部", Value = "" });
                SelectList itemList = new SelectList(list, "Value", "Text");
                ViewBag.UserList = itemList.AsEnumerable<SelectListItem>();
            }
            else
            {
                ViewBag.UserList = new List<SelectListItem>
                {
                    new SelectListItem { Text = user.UserName, Value = user.UserId.ToString() }
                };
            }

            return View();
        }

        public ActionResult Show(int id)
        {
            var s = _studentsService.GetEntity(id);
            var model = new StudentViewModel {
                Id = s.Id,
                SurName = s.SurName,
                Sex = s.Sex == 0 ? "女" : "男",
                Birthday = s.Birthday,
                Period = s.Period,
                ExaminationLevel = s.ExaminationLevel,
                MajorName = s.MajorName,
                Nationality = s.Nationality,
                PoliticalStatus = s.PoliticalStatus,
                EducationalLevel = s.EducationalLevel,
                Address = s.Address,
                Phone = s.Phone,
                Remark = s.Remark,
                TestFreeCondition = s.TestFreeCondition,
                IdCardFrontPath = s.IdCardFrontPath,
                IdCardBackPath = s.IdCardBackPath,
                BareheadedPhotoPath = s.BareheadedPhotoPath,
                Status = s.Status
            };
            return View(model);
        }

        public ActionResult Add()
        {
            List<SelectListItem> eduItems = new List<SelectListItem>
            {
                new SelectListItem{ Text = "无", Value = "0", Selected = true },
                new SelectListItem{ Text = "初中", Value = "1" },
                new SelectListItem{ Text = "高中", Value = "2" },
                new SelectListItem{ Text = "中专", Value = "3" },
                new SelectListItem{ Text = "大专在读", Value = "4" },
                new SelectListItem{ Text = "大专毕业", Value = "5" }
            };
            List<SelectListItem> examItems = new List<SelectListItem>
            {
                new SelectListItem{ Text = "专科", Value = "1" },
                new SelectListItem{ Text = "本科", Value = "2", Selected = true }
            };
            ViewBag.EduLevelItemList = eduItems;
            ViewBag.ExamLevelItemList = examItems;
            return View();
        }

        [HttpPost]
        public ActionResult Add(Students model)
        {            
            UserDto user = ViewBag.User as UserDto;
            model.CreationTime = DateTime.Now;
            model.CreatorUserId = user.UserId;
            
            int result = _studentsService.Add(model);
            if(result > 0)
            {
                return Json(new
                {
                    Status = 1,
                    Message = "新增成功"
                });
            }
            else
            {
                return Json(new
                {
                    Status = 0,
                    Message = "新增失败"
                });
            }
        }
        
        [HttpPost]
        public ActionResult GetStudents(jqDataTableParameter tableParam, string realname, string creator, string status, string start_date, string end_date, string major_name)
        {
            UserDto user = ViewBag.User as UserDto;

            #region 组合查询条件
            var whereExp = PredicateBuilder.True<Students>();
            whereExp = whereExp.And(p => !p.IsDeleted);
            //创建人
            if (ViewBag.RoleId != 0)
                whereExp = whereExp.And(p => p.CreatorUserId == user.UserId);
            else
            {
                if (!string.IsNullOrEmpty(creator))
                    whereExp = whereExp.And(p => p.CreatorUserId == int.Parse(creator));
            }
            //学生姓名
            if (!string.IsNullOrEmpty(realname))
                whereExp = whereExp.And(p => p.SurName == realname.Trim());
            //审核状态
            if (!string.IsNullOrEmpty(status))
                whereExp = whereExp.And(p => p.Status == bool.Parse(status));
            //专业名称
            if (!string.IsNullOrEmpty(major_name))
                whereExp = whereExp.And(p => p.MajorName == major_name);
            //创建时间
            if (!string.IsNullOrEmpty(start_date))
                whereExp = whereExp.And(p => p.CreationTime >= DateTime.Parse(start_date));
            if (!string.IsNullOrEmpty(end_date))
                whereExp = whereExp.And(p => p.CreationTime <= DateTime.Parse(end_date));

            ViewBag.Exp = whereExp;
            #endregion


            //1.0 首先获取datatable提交过来的参数
            string echo = tableParam.sEcho;  //用于客户端自己的校验
            int displayStart = tableParam.iDisplayStart;//要请求的该页第一条数据的序号
            int pageSize = tableParam.iDisplayLength;//每页容量（=-1表示取全部数据）

            var total = _studentsService.GetDataListBy(whereExp).Count();
            var students = _studentsService.GetPagedList<DateTime>(displayStart, pageSize, whereExp, p => p.CreationTime, false);
            var data = (from s in students
                       select new StudentOutput
                       {
                           Id = s.Id,
                           SurName = s.SurName,
                           Sex = s.Sex,
                           Period = s.Period,
                           ExaminationLevel = s.ExaminationLevel,
                           MajorName = s.MajorName,
                           Nationality = s.Nationality,
                           EducationalLevel = s.EducationalLevel,
                           Address = s.Address,
                           Phone = s.Phone,
                           Remark = s.Remark,
                           Creator = s.Users.RealName,
                           CreationTime = s.CreationTime,
                           Status = s.Status
                       }).ToList();
            return Json(new
            {
                sEcho = echo,
                iTotalRecords = total,
                iTotalDisplayRecords = total,
                aaData = data
            });
        }

        public FileResult Export()
        {
            Expression<Func<Students, bool>> expression = ViewBag.Exp as Expression<Func<Students, bool>>;
            var students = _studentsService.GetDataListBy(expression);
            return File();
        }
    }
}