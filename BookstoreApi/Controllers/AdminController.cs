namespace BookstoreApi.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController(
    ISchedulerFactory schedFactory,
    ILogger<AdminController> logger) : ControllerBase
{
    private const string ImportGroup = "ImportGroup";
    private const string JobName = "BookImportJob";

    private readonly ISchedulerFactory _schedFactory = schedFactory;
    private readonly ILogger<AdminController> _logger = logger;

    [HttpPost("trigger-import")]
    [Authorize(Policy = AuthPolicies.RequireReadWriteRole)]
    public async Task<IActionResult> TriggerImport()
    {
        var scheduler = await _schedFactory.GetScheduler();
        var jobKey = new JobKey(JobName, ImportGroup);

        _logger.LogInformation("Manual trigger requested for job {JobKey}", jobKey);

        if (!await scheduler.CheckExists(jobKey))
        {
            return NotFound(new { Message = $"Job '{jobKey.Name}' not found." });
        }

        await scheduler.TriggerJob(jobKey);

        return Ok(new { Message = "BookImportJob triggered." });
    }

    [HttpGet("trigger-status")]
    [Authorize(Policy = AuthPolicies.RequireReadWriteRole)]
    public async Task<IActionResult> GetTriggerStatus()
    {
        var scheduler = await _schedFactory.GetScheduler();
        var triggerKeys = await scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.GroupEquals(ImportGroup));
        var list = new List<object>();

        foreach (var tKey in triggerKeys)
        {
            var trigger = await scheduler.GetTrigger(tKey);
            if (trigger == null)
            {
                list.Add(new { tKey.Name, Exists = false, Message = "Trigger vanished" });
                continue;
            }

            var state = await scheduler.GetTriggerState(tKey);

            list.Add(new
            {
                tKey.Name,
                Exists = true,
                JobName = trigger.JobKey.Name,
                NextFireTime = trigger.GetNextFireTimeUtc()?.UtcDateTime,
                PreviousFireTime = trigger.GetPreviousFireTimeUtc()?.UtcDateTime,
                State = state.ToString()
            });
        }

        return Ok(list);
    }
}
