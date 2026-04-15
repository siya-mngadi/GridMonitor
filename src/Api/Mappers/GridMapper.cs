using GridMonitor.Api.Requests;
using GridMonitor.Api.Responses;
using GridMonitor.Domain.Entities;

namespace GridMonitor.Api.Mappers;

public class GridMapper
{
	public SubscriptionResponse ToResponse(AlertSubscription subscription)
	{
		return new SubscriptionResponse(
			subscription.Id,
			subscription.SuburbId,
			subscription.Suburb.Name,
			subscription.AlertMinutesBefore,
			subscription.Active,
			subscription.Channels.Select(c => new ChannelResponse(
				c.Id,
				c.ChannelType,
				c.Destination,
				c.Active
			))
		);
	}

	public IEnumerable<SubscriptionResponse> ToResponse(IEnumerable<AlertSubscription> subscriptions)
	{
		return subscriptions.Select(ToResponse);
	}

	public SuburbSearchResult ToResponse(Suburb suburb)
	{
		return new SuburbSearchResult(suburb.Id, suburb.Name, suburb.Municipality?.Name);
	}

	public IEnumerable<SuburbSearchResult> ToResponse(IEnumerable<Suburb> suburbs)
	{
		return suburbs.Select(ToResponse);
	}

	public ChannelResponse ToResponse(AlertChannel channel)
	{
		return new ChannelResponse(
			channel.Id,
			channel.ChannelType,
			channel.Destination,
			channel.Active
		);
	}

	public IEnumerable<ChannelResponse> ToResponse(IEnumerable<AlertChannel> channels)
	{
		return channels.Select(ToResponse);
	}

	public SuburbScheduleResponse ToResponse(SuburbSchedule schedule)
	{
		return new SuburbScheduleResponse(
			schedule.SuburbName,
			schedule.CurrentStage,
			schedule.UpcomingSlots.Select(s => new ScheduleSlotResponse(
				s.SuburbId,
				s.Stage,
				s.StartTime,
				s.EndTime,
				s.ScheduleDay,
				s.CreatedAt
			))
		);
	}

	public IEnumerable<SuburbScheduleResponse> ToResponse(IEnumerable<SuburbSchedule> schedules)
	{
		return schedules.Select(ToResponse);
	}

	public StageResponse ToResponse(StageSnapshot stage)
	{
		return new StageResponse(stage.Stage, stage.CreatedAt);
	}

	public ApiKeyResponse ToResponse(ApiKey key)
	{
		return new ApiKeyResponse(key.Id, key.KeyPrefix, key.CreatedAt, key.DailyCallLimit);
	}

	public User FromRequest(RegisterRequest request)
	{
		return new User
		{
			Email = request.Email,
			Password = request.Password,
			CreatedAt = DateTime.UtcNow,
		};
	}

	public AlertChannel FromRequest(AddChannelRequest request)
	{
		return new AlertChannel
		{
			ChannelType = request.Type,
			Destination = request.Destination,
			Active = true
		};
	}

	public AlertSubscription FromRequest(SubscribeRequest request)
	{
		return new AlertSubscription
		{
			AlertMinutesBefore = request.AlertMinutesBefore,
			SuburbId = request.SuburbId
		};
	}
}
