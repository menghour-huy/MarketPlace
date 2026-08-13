# HttpClient Timeout Exception - Root Cause and Fix

## Issue Summary
The application was throwing a `TaskCanceledException` with the message: 
**"The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing."**

This occurred when the Market.Client application attempted to retrieve user profile data from the Market.API service.

## Root Cause
The HttpClient timeout was configured to **30 seconds** (in `Market.Client.API/DependencyInjection.cs` and `appsettings.json`), but the API request was taking longer than 30 seconds to complete. This caused the HttpClient to cancel the request before the API could respond.

### Why Was It Taking So Long?
Based on the stack trace and code analysis:
1. The `GetUserProfileAsync` endpoint in the API is simple and should execute quickly
2. The delay is likely caused by:
   - **Database connection pool exhaustion or slow database queries**
   - **Network latency between services**
   - **Authentication/Authorization middleware delays**
   - **API not responding or deadlocked**

## Fixes Applied

### 1. Increased HttpClient Timeout (Primary Fix)
- **Changed default timeout from 30 to 100 seconds** in `Market.Client.API/DependencyInjection.cs`
- Updated `appsettings.json` and `appsettings.Development.json` to reflect the new 100-second timeout
- Added explanatory comments to document the change

**Files Modified:**
- `Market.Client.API/DependencyInjection.cs`
- `Market.Client/appsettings.json`
- `Market.Client/appsettings.Development.json`

### 2. Improved Error Handling
Enhanced `UserApiClient.cs` to provide better error messages for different timeout scenarios:
- **TaskCanceledException with TimeoutException**: "The request timed out. Please try again or contact support if the issue persists."
- **HttpRequestException**: "Unable to connect to the server. Please check your connection and try again."
- **Generic exceptions**: Existing generic error message

**Files Modified:**
- `Market.Client.API/Service/User/UserApiClient.cs`

### 3. Added Performance Monitoring
Added diagnostic logging to identify slow operations:

**In `Market.API.Persistence/Identity/IdentityService.cs`:**
- Added `Stopwatch` to track `GetUserProfileAsync` execution time
- Logs a warning if database lookup takes more than 1 second

**In `Market.API/Controllers/UserController.cs`:**
- Added `Stopwatch` to track full request time
- Logs start, completion, and warnings for slow requests (>1 second)
- Added detailed logging for success and failure cases

**Files Modified:**
- `Market.API.Persistence/Identity/IdentityService.cs`
- `Market.API/Controllers/UserController.cs`

## Benefits of These Changes

1. **Immediate Relief**: The 100-second timeout gives the API more time to respond, preventing premature request cancellations
2. **Better User Experience**: Users now receive specific, actionable error messages when timeouts occur
3. **Improved Observability**: Performance monitoring logs will help identify the root cause of slow API responses
4. **Future-Proofing**: The timeout is now configurable via `ApiSettings:TimeoutSeconds` in appsettings.json

## Next Steps (Recommended)

1. **Monitor the logs** to identify why the API is taking 30+ seconds to respond
2. **Check database performance**: 
   - Verify connection pool settings
   - Check for missing indexes
   - Review query execution plans
3. **Review authentication middleware** for potential bottlenecks
4. **Consider implementing request timeouts at the API level** to fail fast instead of letting requests hang
5. **Add health checks** to detect when the API is unhealthy or unresponsive
6. **Implement circuit breaker patterns** (already configured via Polly in DependencyInjection.cs)

## Testing
After applying these fixes:
1. Restart both Market.Client and Market.API applications
2. Test the user settings page that was previously timing out
3. Monitor the logs for any slow request warnings
4. If timeouts still occur with the 100-second timeout, investigate the API performance issues identified in the logs
