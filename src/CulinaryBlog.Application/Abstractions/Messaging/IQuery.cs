using MediatR;

namespace CulinaryBlog.Application.Abstractions.Messaging;

public interface IQuery<out TResponse> : IRequest<TResponse>;
