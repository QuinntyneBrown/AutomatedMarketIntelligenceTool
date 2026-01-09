# Branch Protection Setup Guide

This document describes how to configure branch protection rules for the `main` branch to enforce CI/CD checks before merging pull requests.

## Prerequisites

- Repository admin access
- PR Validation workflow configured (`.github/workflows/pr-validation.yml`)

## Setup Instructions

### Via GitHub Web Interface

1. Navigate to your repository on GitHub
2. Go to **Settings** > **Branches**
3. Under "Branch protection rules", click **Add rule**
4. Configure the following settings:

#### Branch Name Pattern
```
main
```

#### Protection Settings

**Require a pull request before merging:**
- ✅ Enable this option
- Optionally enable "Require approvals" (recommended: 1 approval)

**Require status checks to pass before merging:**
- ✅ Enable this option
- ✅ Require branches to be up to date before merging
- **Add the following status checks:**
  - `Build and Test Backend (.NET)`
  - `Build and Test Frontend (Angular)`
  - `All Checks Passed`

**Other recommended settings:**
- ✅ Require conversation resolution before merging
- ✅ Do not allow bypassing the above settings

5. Click **Create** to save the rule

### Via GitHub CLI (gh)

```bash
# Install GitHub CLI if not already installed: https://cli.github.com/

# Create branch protection rule
gh api repos/:owner/:repo/branches/main/protection \
  --method PUT \
  --field required_status_checks[strict]=true \
  --field required_status_checks[contexts][]=Build and Test Backend (.NET) \
  --field required_status_checks[contexts][]=Build and Test Frontend (Angular) \
  --field required_status_checks[contexts][]=All Checks Passed \
  --field required_pull_request_reviews[required_approving_review_count]=1 \
  --field required_pull_request_reviews[dismiss_stale_reviews]=true \
  --field required_conversation_resolution=true \
  --field enforce_admins=false
```

Replace `:owner` and `:repo` with your GitHub username/organization and repository name.

## What This Protects Against

With these branch protection rules enabled:

1. **No direct pushes to main**: All changes must go through a pull request
2. **Build verification**: Code must compile successfully for both backend and frontend
3. **Test verification**: All unit tests must pass before merging
4. **Code review**: Ensures at least one team member has reviewed the changes (if approvals enabled)
5. **Up-to-date branches**: PRs must be updated with latest main before merging

## Testing the Protection

After setting up branch protection:

1. Create a new branch and make a change
2. Open a pull request against `main`
3. Verify that:
   - The PR Validation workflow runs automatically
   - You cannot merge until all checks pass
   - The status checks are clearly visible in the PR interface

## Troubleshooting

### Status checks not appearing

- Ensure the workflow has run at least once on a PR
- Verify the job names in the workflow file match exactly
- Wait a few minutes for GitHub to register the new status checks

### Cannot merge despite passing checks

- Verify you have the correct permissions
- Check if there are any branch protection settings preventing the merge
- Ensure the branch is up to date with `main`

## Local Development

To ensure your code will pass CI checks before pushing, run:

```bash
# Backend
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release

# Frontend
cd src/AutomatedMarketIntelligenceTool.WebApp
npm ci
npm run build
npm test -- --watch=false
```

## References

- [GitHub Branch Protection Documentation](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches)
- [GitHub Actions Status Checks](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/collaborating-on-repositories-with-code-quality-features/about-status-checks)
