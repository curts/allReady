﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using AllReady.Models;

namespace AllReady.ViewModels
{
    public class EventViewModel
    {
        public EventViewModel()
        {
        }

        public EventViewModel(Event campaignEvent)
        {
            Id = campaignEvent.Id;
            if (campaignEvent.Campaign != null)
            {
                CampaignId = campaignEvent.Campaign.Id;
                CampaignName = campaignEvent.Campaign.Name;
                TimeZoneId = campaignEvent.Campaign.TimeZoneId;
                if (campaignEvent.Campaign.ManagingOrganization != null)
                {
                    OrganizationId = campaignEvent.Campaign.ManagingOrganization.Id;
                    OrganizationName = campaignEvent.Campaign.ManagingOrganization.Name;
                    HasPrivacyPolicy = !string.IsNullOrEmpty(campaignEvent.Campaign.ManagingOrganization.PrivacyPolicy);
                }
            }

            Title = campaignEvent.Name;
            Description = campaignEvent.Description;
            EventType = campaignEvent.EventType;
            StartDateTime = campaignEvent.StartDateTime;
            EndDateTime = campaignEvent.EndDateTime;

            if (campaignEvent.Location != null)
            {
                Location = new LocationViewModel(campaignEvent.Location);
            }

            IsClosed = EndDateTime.UtcDateTime < DateTimeOffset.UtcNow;

            ImageUrl = campaignEvent.ImageUrl;

            //TODO Location
            Tasks = campaignEvent.Tasks != null
                 ? new List<TaskViewModel>(campaignEvent.Tasks.Select(data => new TaskViewModel(data)).OrderBy(task => task.StartDateTime))
                 : new List<TaskViewModel>();

            SignupModel = new EventSignupViewModel();

            RequiredSkills = campaignEvent.RequiredSkills?.Select(acsk => acsk.Skill).ToList();
            IsLimitVolunteers = campaignEvent.IsLimitVolunteers;
            IsAllowWaitList = campaignEvent.IsAllowWaitList;
            
        }

        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public string OrganizationName { get; set; }
        public int CampaignId { get; set; }
        public string CampaignName { get; set; }
        public string Title { get; set; }
        public EventTypes EventType { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string TimeZoneId { get; set; }
        public DateTimeOffset StartDateTime { get; set; }
        public DateTimeOffset EndDateTime { get; set; }
        public LocationViewModel Location { get; set; }
        public List<TaskViewModel> Tasks { get; set; } = new List<TaskViewModel>();
        public List<TaskViewModel> UserTasks { get; set; } = new List<TaskViewModel>();
        public bool IsUserVolunteeredForEvent { get; set; }
        public List<ApplicationUser> Volunteers { get; set; }
        public string UserId { get; set; }
        public List<Skill> RequiredSkills { get; set; }
        public List<Skill> UserSkills { get; set; }
        public int NumberOfVolunteersRequired { get; set; }
        public EventSignupViewModel SignupModel { get; set; }
        public bool IsClosed { get; set; }
        public bool HasPrivacyPolicy { get; set; }
        public List<EventSignup> UsersSignedUp { get; set; } = new List<EventSignup>();
        public bool IsLimitVolunteers { get; set; } = true;
        public bool IsAllowWaitList { get; set; } = true;
        public int NumberOfUsersSignedUp => UsersSignedUp.Count;
        public bool IsFull => NumberOfUsersSignedUp >= NumberOfVolunteersRequired;
        public bool IsAllowSignups => !IsLimitVolunteers || !IsFull || IsAllowWaitList;
    }

    public static class EventViewModelExtension
    {
        public static LocationViewModel ToViewModel(this Location location)
        {
            var value = new LocationViewModel
            {
                Address1 = location.Address1,
                Address2 = location.Address2,
                City = location.City,
                PostalCode = location.PostalCode,
                State = location.State
            };
            return value;
        }
        public static Location ToModel(this LocationViewModel location)
        {
            var value = new Location
            {
                Address1 = location.Address1,
                Address2 = location.Address2,
                City = location.City,
                PostalCode = location.PostalCode,
                State = location.State,
                Country = "TODO:  Put country in both objects"
            };
            return value;
        }
        public static IEnumerable<EventViewModel> ToViewModel(this IEnumerable<Event> campaignEvents)
        {
            return campaignEvents.Select(campaignEvent => new EventViewModel(campaignEvent));
        }

        public static EventViewModel WithUserInfo(this EventViewModel viewModel, Event campaignEvent, ClaimsPrincipal user, IAllReadyDataAccess dataAccess)
        {
            if (user.IsSignedIn())
            {
                var userId = user.GetUserId();
                var appUser = dataAccess.GetUser(userId);
                viewModel.UserId = userId;
                viewModel.UserSkills = appUser?.AssociatedSkills?.Select(us => us.Skill).ToList();
                viewModel.IsUserVolunteeredForEvent = dataAccess.GetEventSignups(viewModel.Id, userId).Any();
                var assignedTasks = campaignEvent.Tasks.Where(t => t.AssignedVolunteers.Any(au => au.User.Id == userId)).ToList();
                viewModel.UserTasks = new List<TaskViewModel>(assignedTasks.Select(data => new TaskViewModel(data, userId)).OrderBy(task => task.StartDateTime));
                var unassignedTasks = campaignEvent.Tasks.Where(t => t.AssignedVolunteers.All(au => au.User.Id != userId)).ToList();
                viewModel.Tasks = new List<TaskViewModel>(unassignedTasks.Select(data => new TaskViewModel(data, userId)).OrderBy(task => task.StartDateTime));
                viewModel.SignupModel = new EventSignupViewModel()
                {
                    EventId = viewModel.Id,
                    UserId = userId,
                    Name = appUser.Name,
                    PreferredEmail = appUser.Email,
                    PreferredPhoneNumber = appUser.PhoneNumber
                };
            }
            else
            {
                viewModel.UserTasks = new List<TaskViewModel>();
            }
            return viewModel;
        }
    }
}
