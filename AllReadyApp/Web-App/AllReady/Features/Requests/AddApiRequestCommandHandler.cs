﻿using System;
using System.Linq;
using System.Threading.Tasks;
using AllReady.Extensions;
using AllReady.Models;
using MediatR;
using Geocoding;
using Microsoft.EntityFrameworkCore;

namespace AllReady.Features.Requests
{
    public class AddApiRequestCommandHandler : IAsyncRequestHandler<AddApiRequestCommand, Request>
    {
        private readonly AllReadyContext _context;
        private readonly IGeocoder _geocoder;

        public Func<Guid> NewRequestId = () => Guid.NewGuid();
        public Func<DateTime> DateTimeUtcNow = () => DateTime.UtcNow;

        public AddApiRequestCommandHandler(AllReadyContext context, IGeocoder geocoder)
        {
            _context = context;
            _geocoder = geocoder;
        }

        public async Task<Request> Handle(AddApiRequestCommand message)
        {
            var requestId = GetRequestId(message.RequestViewModel.RequestId);

            var request = await _context.Requests.SingleOrDefaultAsync(x => x.RequestId == requestId) ?? new Request { RequestId = requestId };
            request.ProviderId = message.RequestViewModel.ProviderId;
            request.ProviderData = message.RequestViewModel.ProviderData;
            request.Address = message.RequestViewModel.Address;
            request.City = message.RequestViewModel.City;
            request.DateAdded = DateTimeUtcNow();
            request.Email = message.RequestViewModel.Email;
            request.Name = message.RequestViewModel.Name;
            request.Phone = message.RequestViewModel.Phone;
            request.State = message.RequestViewModel.State;
            request.Zip = message.RequestViewModel.Zip;
            request.Status = ConvertRequestStatusToEnum(message.RequestViewModel.Status);
            request.Source = RequestSource.Api;

            var address = _geocoder.Geocode(message.RequestViewModel.Address, message.RequestViewModel.City, message.RequestViewModel.State, message.RequestViewModel.Zip, string.Empty).FirstOrDefault();
            request.Latitude = message.RequestViewModel.Latitude == 0 ? address?.Coordinates.Latitude ?? 0 : message.RequestViewModel.Latitude;
            request.Longitude = message.RequestViewModel.Longitude == 0 ? address?.Coordinates.Longitude ?? 0 : message.RequestViewModel.Longitude;

            _context.AddOrUpdate(request);
            await _context.SaveChangesAsync();

            //TODO mgmccarthy: find out if/why we need to return the entire Request back to the caller. I would rather us return the RequestId
            return request;
        }

        private Guid GetRequestId(string requestId)
        {
            if (string.IsNullOrEmpty(requestId))
            {
                return NewRequestId();
            }

            return Guid.Parse(requestId);
        }

        private static RequestStatus ConvertRequestStatusToEnum(string stringRequestStatus)
        {
            return (RequestStatus) Enum.Parse(typeof(RequestStatus), stringRequestStatus);
        }
    }
}