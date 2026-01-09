# Unimplemented Requirements Analysis

**Date:** January 9, 2026
**Overall Status:** Phase 5 (all 7 sprints) is substantially complete (~95%). This document identifies remaining gaps.

---

## Summary

| Category | Count | Status |
|----------|-------|--------|
| Not Implemented | 4 | Blocking or missing functionality |
| Partially Implemented | 4 | Framework exists, completion needed |
| **Total Gaps** | **8** | |

---

## Not Implemented

### 1. Email Notifications

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | P4-OBJ-09 / REQ-RP-012 |
| **Status** | Framework only, NOT IMPLEMENTED |
| **Location** | `src/AutomatedMarketIntelligenceTool.Core/Services/AlertService.cs` (lines 168-170) |
| **Impact** | Alert system cannot send email notifications despite API support |

**Details:**
- `NotificationMethod.Email` case exists but has a TODO comment
- Logs warning and marks notification as failed
- No SMTP service integration
- Service interfaces define email support but no implementation

**Related Files:**
- `Core/Services/AlertService.cs` - TODO at line 168
- `Core/Services/IAlertService.cs` - Interface defines `NotificationMethod.Email`

---

### 2. Webhook Notifications

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | P4-OBJ-09 / REQ-RP-012 |
| **Status** | Framework only, NOT IMPLEMENTED |
| **Location** | `src/AutomatedMarketIntelligenceTool.Core/Services/AlertService.cs` (lines 173-176) |
| **Impact** | Alert system cannot send webhook notifications despite API support |

**Details:**
- `NotificationMethod.Webhook` case exists but has a TODO comment
- Logs warning and marks notification as failed
- No HTTP client implementation for webhook delivery
- Service interfaces define webhook support but no implementation

**Related Files:**
- `Core/Services/AlertService.cs` - TODO at line 174
- `Core/Services/IAlertService.cs` - Interface defines `NotificationMethod.Webhook`

---

### 3. Tenant Context Injection

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | Multi-tenant Enterprise Feature |
| **Status** | Partial, using fallback `Guid.Empty` |
| **Location** | `src/AutomatedMarketIntelligenceTool.Infrastructure/AutomatedMarketIntelligenceToolContext.cs` (lines 49-51) |
| **Impact** | Multi-tenant row-level isolation not functional in production |

**Details:**
- DbContext has TODO comment indicating `ITenantContext` injection not implemented
- Currently hardcoded to use `Guid.Empty` as default tenant
- Impacts data isolation in multi-tenant scenarios
- All queries will use the same tenant ID, breaking isolation

**Related Files:**
- `Infrastructure/AutomatedMarketIntelligenceToolContext.cs` - TODO at line 49
- All entities reference `TenantId` but context can't determine current tenant

---

### 4. CLI Header Configuration Integration

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | P5-OBJ-01 / REQ-WS-013, AC-013.1-013.4 |
| **Status** | Service implemented, CLI integration incomplete |
| **Location** | CLI Commands missing `--header` option |
| **Impact** | Users cannot configure custom headers via CLI despite service being complete |

**Details:**
- `HeaderConfigurationService` fully implemented with 100% test coverage
- Supports Accept, Accept-Language, Referer, DNT headers
- CLI Commands do not expose `--header` option
- Service is available in infrastructure but not integrated into search/scrape commands

**Related Files:**
- `Infrastructure/Services/Headers/HeaderConfigurationService.cs` - Fully implemented
- `Infrastructure/Services/Headers/IHeaderConfigurationService.cs` - Interface defined
- `Cli/Commands/ScrapeCommand.cs` - Missing `--header` option integration

---

## Partially Implemented

### 5. Scheduled Report Email Delivery

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | P5-OBJ-07 / REQ-RP-014 |
| **Status** | Service framework exists, email delivery incomplete |
| **Location** | `src/AutomatedMarketIntelligenceTool.Core/Services/Scheduling/` |
| **Impact** | Scheduled reports can be created but cannot be automatically emailed |

**Details:**
- `ScheduledReportService` and `ReportSchedulerService` classes exist
- Report generation works (HTML, PDF, Excel)
- Email notification for scheduled reports depends on email notification feature (#1)
- Background job scheduling framework in place but may need background service wiring

**Related Files:**
- `Core/Services/Scheduling/ScheduledReportService.cs`
- `Core/Services/Scheduling/ReportSchedulerService.cs`
- `Core/Services/Scheduling/IScheduledReportService.cs`

---

### 6. Multi-Tenant Data Isolation

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | Enterprise Feature |
| **Status** | Data model complete, context injection incomplete |
| **Impact** | Multi-tenant deployments will not have proper data isolation |

**Details:**
- All entities have `TenantId` property
- Database schema supports multi-tenancy
- Row-level security not enforced due to missing `ITenantContext` injection
- Search queries don't filter by current tenant

**Dependency:** Requires completion of item #3 (Tenant Context Injection)

---

### 7. Web App Implementation

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | REQ-FE-001 through REQ-FE-008 |
| **Status** | Minimal - needs verification |
| **Location** | `src/AutomatedMarketIntelligenceTool.WebApp/` |
| **Impact** | Unknown feature parity with CLI |

**Details:**
- Angular project structure in place (`angular.json`, `package.json`)
- Limited TypeScript files found (app.ts, app.config.ts, app.routes.ts, main.ts)
- No comprehensive component list found
- Unclear if full feature parity with CLI exists

**Recommendation:** Review Angular components and features to identify gaps

---

### 8. Background Job Scheduling

| Attribute | Details |
|-----------|---------|
| **Requirement ID** | Infrastructure for Scheduled Reports |
| **Status** | Service interfaces defined, hosting integration unclear |
| **Impact** | Scheduled reports may not execute automatically |

**Details:**
- `ReportSchedulerService` exists but may need:
  - Background service registration in DI
  - Hosted service implementation for long-running tasks
  - Cron job execution infrastructure

**Recommendation:** Verify background service is registered in API/CLI startup

---

## Files Requiring Modification

### High Priority

| File | Changes Required |
|------|------------------|
| `Core/Services/AlertService.cs` | Implement email notification case, implement webhook notification case, add SMTP service dependency |
| `Infrastructure/AutomatedMarketIntelligenceToolContext.cs` | Add `ITenantContext` injection, update tenant ID assignment logic |

### Medium Priority

| File | Changes Required |
|------|------------------|
| `Cli/Commands/ScrapeCommand.cs` | Add `--header` option, wire `HeaderConfigurationService` |
| `Cli/Commands/SearchCommand.cs` | Add `--header` option, wire `HeaderConfigurationService` |
| `Core/Services/Scheduling/ReportSchedulerService.cs` | Verify background service registration, implement cron scheduling integration |

---

## Priority Recommendations

### High Priority (Blocking Production)

1. **Implement email notification service** - Required for alert system functionality
2. **Fix TenantContext injection** - Required for multi-tenant data isolation
3. **Implement webhook notification service** - Required for external integrations

### Medium Priority (User-Facing Features)

4. **Integrate CLI `--header` option** - Service complete, just needs CLI wiring
5. **Complete scheduled report email delivery** - Depends on #1

### Low Priority (Infrastructure/Verification)

6. **Verify web app feature coverage** - Audit Angular components
7. **Verify background job scheduling wiring** - Check hosted service registration

---

## Completed Features Reference

The following Phase 5 features are **FULLY IMPLEMENTED**:

| Feature | Sprint | Status |
|---------|--------|--------|
| Request Header Configuration (service only) | S2 | Complete |
| Response Caching | S1 | Complete |
| Batch Deduplication | S1 | Complete |
| Mobile User Agents | S2 | Complete |
| Field Extraction Validation | S2 | Complete |
| Rate Limit Calibration | S2 | Complete |
| HTML Report Generation | S3 | Complete |
| PDF Report Generation | S3 | Complete |
| Excel Report Generation | S3 | Complete |
| Dashboard & Monitoring | S4 | Complete |
| Dealer Analytics | S5 | Complete |
| Relisting Pattern Detection | S5 | Complete |
| Deduplication Audit Trail | S6 | Complete |
| Deduplication Configuration | S6 | Complete |
| Custom Market Regions | S7 | Complete |
| Resource Throttling | S7 | Complete |
| All 10+ Web Scrapers | Phases 1-3 | Complete |
| Image-Based Duplicate Detection | Phase 4 | Complete |
| Concurrent Scraping | Phase 4 | Complete |
| Watch Lists | Phase 4 | Complete |
| Alert System (Console only) | Phase 4 | Complete |
| Import/Export/Backup | Phase 4 | Complete |
