using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using uchat_common.Dtos;

namespace uchat_client.Core.Application.Common.Interfaces;

public interface IContactService
{
    Task<ApiResponse<List<UserDto>>> SearchUsersAsync(string query);
    Task<ApiResponse<List<UserDto>>> GetContactsAsync();
    Task<ApiResponse<bool>> AddContactAsync(string username);
    Task<ApiResponse<bool>> RemoveContactAsync(int userId);

    event EventHandler<UserDto>? ContactAdded;
    event EventHandler<int>? ContactRemoved;
}
