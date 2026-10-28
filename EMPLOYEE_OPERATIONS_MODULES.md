# LeaveFlow Employee Operations Portal

LeaveFlow contains four integrated modules:

1. Leave Management
2. Department Task Management
3. Expense and Reimbursement
4. Performance Appraisal

Shared features include persistent SignalR notifications, audit logging, role-scoped reports, CSV export, expense receipt uploads, reminder background processing, Department authorization and optimistic concurrency columns.

## Database update

Apply the migration on SQL Server:

```powershell
dotnet ef database update `
  --project LeaveFlow.Infrastructure/LeaveFlow.Persistence.csproj `
  --startup-project LeaveFlow.Host/LeaveFlow.Host.csproj
```

## Main workflows

### Tasks

- Manager assigns Employee tasks in the same Department.
- System Admin assigns tasks to Managers.
- Employee starts, updates and completes work.
- Assigner accepts or reopens completed work.

### Expenses

- Only Department Managers create and submit Expense Claims.
- Manager creates a Draft claim, adds itemized expenses and uploads receipts (PDF, PNG, JPG up to 5 MB stored under `App_Data/ExpenseReceipts`).
- Submitted claims route directly to the System Administrator for review (Approve / Reject).

### Performance

- System Admin creates and opens review cycles.
- Opening a cycle generates reviews for active staff employees in each department.
- The system automatically derives **Task Performance Metrics** (total completed tasks, completed on time, completed late, on-time completion %) and **Non-Annual Leave Activity** (total non-annual requests, approved, rejected) within the cycle period.
- Department Managers review observed metrics, write contextual **Manager Review Comments** for each department employee, and submit a consolidated **Department Review Report** (with optional department observation) to Administration.
- System Admin views submitted department reports, employee summaries, and underlying metrics. Admin does not appraise managers.
- Employees view their completed review metrics and manager feedback in read-only mode.

### Notifications

- Notifications are saved to database and pushed to connected browsers through SignalR.
- Offline users retain unread notifications.
- A background worker creates deduplicated task and appraisal reminders every 12 hours.

## Initial Credentials

```text
admin@leaveflow.com
Admin@123
```
