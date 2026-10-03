namespace CulinaryBlog.Application.Auth.Queries.GetCurrentUser;

using CulinaryBlog.Application.Abstractions.Messaging;
using CulinaryBlog.Application.Auth.DTOs;

public sealed record GetCurrentUserQuery : IQuery<AuthUserDto>;
