﻿using FastEndpoints;
using NotificationHub.Services.CurrentApplication;

namespace NotificationHub.Pipeline;

public class ApiIdHeaderPreProcessor : IGlobalPreProcessor
{
	public const string AppIdHeader = "x-app-id";

	private readonly ILogger<ApiIdHeaderPreProcessor> _logger;
	private readonly ICurrentApplication _currentApplication;

	public ApiIdHeaderPreProcessor(ILogger<ApiIdHeaderPreProcessor> logger,
		ICurrentApplication currentApplication)
	{
		_logger = logger;
		_currentApplication = currentApplication;
	}
	
	public Task PreProcessAsync(IPreProcessorContext ctx, CancellationToken ct)
	{
		if (ctx.HttpContext.ResponseStarted())
		{
			return Task.CompletedTask;
		}
		
		if (!ctx.HttpContext.Request.Headers.TryGetValue(AppIdHeader, out var appId))
		{
			_logger.LogWarning("Unauthorized request from {RemoteIpAddress}: missing {AppHeader}", ctx.HttpContext.Connection.RemoteIpAddress, AppIdHeader);
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}

		if (_currentApplication.Application.IsT1)
		{
			_logger.LogWarning("Unauthorized request from {RemoteIpAddress}: {ApplicationId} is not registered", ctx.HttpContext.Connection.RemoteIpAddress, appId);
			return ctx.HttpContext.Response.SendUnauthorizedAsync(cancellation: ct);
		}

		return Task.CompletedTask;
	}
}