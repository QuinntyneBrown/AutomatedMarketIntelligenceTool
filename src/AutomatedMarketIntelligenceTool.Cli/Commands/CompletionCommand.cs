using System.ComponentModel;
using System.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace AutomatedMarketIntelligenceTool.Cli.Commands;

/// <summary>
/// Command to generate shell completion scripts for various shells.
/// </summary>
public class CompletionCommand : Command<CompletionCommand.Settings>
{
    private const string AppName = "car-search";

    public override int Execute(CommandContext context, Settings settings)
    {
        try
        {
            var shell = settings.Shell?.ToLowerInvariant() ?? DetectShell();

            if (string.IsNullOrEmpty(shell))
            {
                AnsiConsole.MarkupLine("[red]Error: Could not detect shell. Please specify --shell option.[/]");
                return ExitCodes.ValidationError;
            }

            var script = shell switch
            {
                "bash" => GenerateBashCompletion(),
                "zsh" => GenerateZshCompletion(),
                "powershell" or "pwsh" => GeneratePowerShellCompletion(),
                "fish" => GenerateFishCompletion(),
                _ => null
            };

            if (script == null)
            {
                AnsiConsole.MarkupLine($"[red]Error: Unsupported shell '{shell}'. Supported shells: bash, zsh, powershell, fish[/]");
                return ExitCodes.ValidationError;
            }

            if (!string.IsNullOrEmpty(settings.OutputPath))
            {
                File.WriteAllText(settings.OutputPath, script);
                AnsiConsole.MarkupLine($"[green]Completion script written to: {settings.OutputPath}[/]");

                // Show installation instructions
                ShowInstallationInstructions(shell, settings.OutputPath);
            }
            else
            {
                // Output script to stdout for piping
                Console.WriteLine(script);
            }

            return ExitCodes.Success;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return ExitCodes.GeneralError;
        }
    }

    private static string? DetectShell()
    {
        var shell = Environment.GetEnvironmentVariable("SHELL");
        if (!string.IsNullOrEmpty(shell))
        {
            if (shell.EndsWith("/bash", StringComparison.OrdinalIgnoreCase))
            {
                return "bash";
            }

            if (shell.EndsWith("/zsh", StringComparison.OrdinalIgnoreCase))
            {
                return "zsh";
            }

            if (shell.EndsWith("/fish", StringComparison.OrdinalIgnoreCase))
            {
                return "fish";
            }
        }

        // Check for PowerShell
        var psModulePath = Environment.GetEnvironmentVariable("PSModulePath");
        if (!string.IsNullOrEmpty(psModulePath))
        {
            return "powershell";
        }

        return null;
    }

    private static string GenerateBashCompletion()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Bash completion script for {AppName}");
        sb.AppendLine($"# Add this to your ~/.bashrc or ~/.bash_completion");
        sb.AppendLine();
        sb.AppendLine($"_{AppName.Replace("-", "_")}_completions()");
        sb.AppendLine("{");
        sb.AppendLine("    local cur prev words cword");
        sb.AppendLine("    _get_comp_words_by_ref -n = cur prev words cword");
        sb.AppendLine();
        sb.AppendLine("    local commands=\"search scrape list export config profile status stats show import backup restore compare review alert watch completion\"");
        sb.AppendLine();
        sb.AppendLine("    # Complete commands at position 1");
        sb.AppendLine("    if [[ ${cword} -eq 1 ]]; then");
        sb.AppendLine("        COMPREPLY=($(compgen -W \"${commands}\" -- \"${cur}\"))");
        sb.AppendLine("        return 0");
        sb.AppendLine("    fi");
        sb.AppendLine();
        sb.AppendLine("    local cmd=\"${words[1]}\"");
        sb.AppendLine();
        sb.AppendLine("    case \"${cmd}\" in");
        sb.AppendLine("        search)");
        sb.AppendLine("            local opts=\"-t --tenant-id -m --make --model --year-min --year-max --price-min --price-max --mileage-max --condition --body-style --drivetrain -f --format -i --interactive -v --verbose -q --quiet\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        scrape)");
        sb.AppendLine("            local opts=\"-t --tenant-id -s --site -m --make --model -p --postal-code -r --radius --max-pages --delay -v --verbose -q --quiet\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        list)");
        sb.AppendLine("            local opts=\"-t --tenant-id -m --make --model --condition --new-only --sort --page --page-size -f --format -v --verbose -q --quiet\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        export)");
        sb.AppendLine("            local opts=\"-t --tenant-id -m --make --model --condition -f --format -v --verbose -q --quiet\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        config)");
        sb.AppendLine("            if [[ ${cword} -eq 2 ]]; then");
        sb.AppendLine("                COMPREPLY=($(compgen -W \"list get set reset path\" -- \"${cur}\"))");
        sb.AppendLine("            fi");
        sb.AppendLine("            ;;");
        sb.AppendLine("        profile)");
        sb.AppendLine("            if [[ ${cword} -eq 2 ]]; then");
        sb.AppendLine("                COMPREPLY=($(compgen -W \"save load list delete\" -- \"${cur}\"))");
        sb.AppendLine("            else");
        sb.AppendLine("                local opts=\"-t --tenant-id -d --description -m --make --model --year-min --year-max --price-min --price-max\"");
        sb.AppendLine("                COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            fi");
        sb.AppendLine("            ;;");
        sb.AppendLine("        backup)");
        sb.AppendLine("            local opts=\"-o --output -l --list --cleanup --retention\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        restore)");
        sb.AppendLine("            local opts=\"-y --yes\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        import)");
        sb.AppendLine("            local opts=\"-t --tenant-id -f --format --dry-run -v --verbose -q --quiet\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("        completion)");
        sb.AppendLine("            local opts=\"-s --shell -o --output\"");
        sb.AppendLine("            if [[ \"${prev}\" == \"-s\" || \"${prev}\" == \"--shell\" ]]; then");
        sb.AppendLine("                COMPREPLY=($(compgen -W \"bash zsh powershell fish\" -- \"${cur}\"))");
        sb.AppendLine("            else");
        sb.AppendLine("                COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            fi");
        sb.AppendLine("            ;;");
        sb.AppendLine("        status|stats|show|compare|review|alert|watch)");
        sb.AppendLine("            local opts=\"-t --tenant-id -v --verbose -q --quiet\"");
        sb.AppendLine("            COMPREPLY=($(compgen -W \"${opts}\" -- \"${cur}\"))");
        sb.AppendLine("            ;;");
        sb.AppendLine("    esac");
        sb.AppendLine();
        sb.AppendLine("    return 0");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"complete -F _{AppName.Replace("-", "_")}_completions {AppName}");

        return sb.ToString();
    }

    private static string GenerateZshCompletion()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"#compdef {AppName}");
        sb.AppendLine();
        sb.AppendLine($"# Zsh completion script for {AppName}");
        sb.AppendLine("# Add this to a file in your $fpath (e.g., ~/.zsh/completions/_car-search)");
        sb.AppendLine();
        sb.AppendLine($"_{AppName.Replace("-", "_")}() {{");
        sb.AppendLine("    local -a commands");
        sb.AppendLine("    commands=(");
        sb.AppendLine("        'search:Search for car listings in the database'");
        sb.AppendLine("        'scrape:Scrape car listings from automotive websites'");
        sb.AppendLine("        'list:List saved car listings from the database'");
        sb.AppendLine("        'export:Export car listings to JSON or CSV files'");
        sb.AppendLine("        'config:Manage application configuration settings'");
        sb.AppendLine("        'profile:Manage search profiles'");
        sb.AppendLine("        'status:Display system status and statistics'");
        sb.AppendLine("        'stats:Display market statistics and analysis'");
        sb.AppendLine("        'show:Display detailed information about a specific listing'");
        sb.AppendLine("        'import:Import car listings from CSV or JSON files'");
        sb.AppendLine("        'backup:Create database backups'");
        sb.AppendLine("        'restore:Restore database from a backup file'");
        sb.AppendLine("        'compare:Compare car listings'");
        sb.AppendLine("        'review:Review queue management'");
        sb.AppendLine("        'alert:Alert management'");
        sb.AppendLine("        'watch:Watch list management'");
        sb.AppendLine("        'completion:Generate shell completion scripts'");
        sb.AppendLine("    )");
        sb.AppendLine();
        sb.AppendLine("    _arguments -C \\");
        sb.AppendLine("        '1:command:->command' \\");
        sb.AppendLine("        '*::args:->args'");
        sb.AppendLine();
        sb.AppendLine("    case $state in");
        sb.AppendLine("        command)");
        sb.AppendLine("            _describe -t commands 'commands' commands");
        sb.AppendLine("            ;;");
        sb.AppendLine("        args)");
        sb.AppendLine("            case $words[1] in");
        sb.AppendLine("                search)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '-t[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '--tenant-id[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '-m[Vehicle make]:make:' \\");
        sb.AppendLine("                        '--make[Vehicle make]:make:' \\");
        sb.AppendLine("                        '--model[Vehicle model]:model:' \\");
        sb.AppendLine("                        '--year-min[Minimum year]:year:' \\");
        sb.AppendLine("                        '--year-max[Maximum year]:year:' \\");
        sb.AppendLine("                        '--price-min[Minimum price]:price:' \\");
        sb.AppendLine("                        '--price-max[Maximum price]:price:' \\");
        sb.AppendLine("                        '--mileage-max[Maximum mileage]:mileage:' \\");
        sb.AppendLine("                        '--condition[Vehicle condition]:condition:(New Used Certified)' \\");
        sb.AppendLine("                        '--body-style[Body style]:style:(sedan suv truck coupe hatchback van wagon convertible)' \\");
        sb.AppendLine("                        '--drivetrain[Drivetrain]:drivetrain:(FWD RWD AWD 4WD)' \\");
        sb.AppendLine("                        '-f[Output format]:format:(table json csv)' \\");
        sb.AppendLine("                        '--format[Output format]:format:(table json csv)' \\");
        sb.AppendLine("                        '-i[Interactive mode]' \\");
        sb.AppendLine("                        '--interactive[Interactive mode]' \\");
        sb.AppendLine("                        '-v[Verbose output]' \\");
        sb.AppendLine("                        '-q[Quiet mode]'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                scrape)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '-t[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '--tenant-id[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '-s[Website to scrape]:site:(autotrader kijiji cargurus all)' \\");
        sb.AppendLine("                        '--site[Website to scrape]:site:(autotrader kijiji cargurus all)' \\");
        sb.AppendLine("                        '-m[Vehicle make]:make:' \\");
        sb.AppendLine("                        '--make[Vehicle make]:make:' \\");
        sb.AppendLine("                        '--model[Vehicle model]:model:' \\");
        sb.AppendLine("                        '-p[Postal code]:postal code:' \\");
        sb.AppendLine("                        '--postal-code[Postal code]:postal code:' \\");
        sb.AppendLine("                        '-r[Search radius]:radius:' \\");
        sb.AppendLine("                        '--radius[Search radius]:radius:' \\");
        sb.AppendLine("                        '--max-pages[Maximum pages to scrape]:pages:' \\");
        sb.AppendLine("                        '--delay[Delay between requests]:delay:'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                backup)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '-o[Output file path]:file:_files' \\");
        sb.AppendLine("                        '--output[Output file path]:file:_files' \\");
        sb.AppendLine("                        '-l[List backups]' \\");
        sb.AppendLine("                        '--list[List backups]' \\");
        sb.AppendLine("                        '--cleanup[Clean up old backups]' \\");
        sb.AppendLine("                        '--retention[Number of backups to keep]:count:'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                restore)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '1:backup file:_files -g \"*.db\"' \\");
        sb.AppendLine("                        '-y[Skip confirmation]' \\");
        sb.AppendLine("                        '--yes[Skip confirmation]'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                import)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '1:import file:_files -g \"*.{csv,json}\"' \\");
        sb.AppendLine("                        '-t[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '--tenant-id[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '-f[File format]:format:(csv json)' \\");
        sb.AppendLine("                        '--format[File format]:format:(csv json)' \\");
        sb.AppendLine("                        '--dry-run[Preview import without saving]'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                config)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '1:operation:(list get set reset path)'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                profile)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '1:action:(save load list delete)' \\");
        sb.AppendLine("                        '-t[Tenant ID]:tenant id:' \\");
        sb.AppendLine("                        '--tenant-id[Tenant ID]:tenant id:'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("                completion)");
        sb.AppendLine("                    _arguments \\");
        sb.AppendLine("                        '-s[Shell type]:shell:(bash zsh powershell fish)' \\");
        sb.AppendLine("                        '--shell[Shell type]:shell:(bash zsh powershell fish)' \\");
        sb.AppendLine("                        '-o[Output file]:file:_files' \\");
        sb.AppendLine("                        '--output[Output file]:file:_files'");
        sb.AppendLine("                    ;;");
        sb.AppendLine("            esac");
        sb.AppendLine("            ;;");
        sb.AppendLine("    esac");
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine($"_{AppName.Replace("-", "_")} \"$@\"");

        return sb.ToString();
    }

    private static string GeneratePowerShellCompletion()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# PowerShell completion script for {AppName}");
        sb.AppendLine("# Add this to your $PROFILE or dot-source it");
        sb.AppendLine();
        sb.AppendLine($"Register-ArgumentCompleter -Native -CommandName {AppName} -ScriptBlock {{");
        sb.AppendLine("    param($wordToComplete, $commandAst, $cursorPosition)");
        sb.AppendLine();
        sb.AppendLine("    $commands = @{");
        sb.AppendLine("        'search' = 'Search for car listings in the database'");
        sb.AppendLine("        'scrape' = 'Scrape car listings from automotive websites'");
        sb.AppendLine("        'list' = 'List saved car listings from the database'");
        sb.AppendLine("        'export' = 'Export car listings to JSON or CSV files'");
        sb.AppendLine("        'config' = 'Manage application configuration settings'");
        sb.AppendLine("        'profile' = 'Manage search profiles'");
        sb.AppendLine("        'status' = 'Display system status and statistics'");
        sb.AppendLine("        'stats' = 'Display market statistics and analysis'");
        sb.AppendLine("        'show' = 'Display detailed information about a specific listing'");
        sb.AppendLine("        'import' = 'Import car listings from CSV or JSON files'");
        sb.AppendLine("        'backup' = 'Create database backups'");
        sb.AppendLine("        'restore' = 'Restore database from a backup file'");
        sb.AppendLine("        'compare' = 'Compare car listings'");
        sb.AppendLine("        'review' = 'Review queue management'");
        sb.AppendLine("        'alert' = 'Alert management'");
        sb.AppendLine("        'watch' = 'Watch list management'");
        sb.AppendLine("        'completion' = 'Generate shell completion scripts'");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    $elements = $commandAst.CommandElements");
        sb.AppendLine("    $command = $null");
        sb.AppendLine("    if ($elements.Count -gt 1) {");
        sb.AppendLine("        $command = $elements[1].Value");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    # Complete commands");
        sb.AppendLine("    if ($elements.Count -le 2 -or ($elements.Count -eq 2 -and $wordToComplete -ne '')) {");
        sb.AppendLine("        $commands.Keys | Where-Object { $_ -like \"$wordToComplete*\" } | ForEach-Object {");
        sb.AppendLine("            [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $commands[$_])");
        sb.AppendLine("        }");
        sb.AppendLine("        return");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    # Command-specific completions");
        sb.AppendLine("    $opts = @()");
        sb.AppendLine("    switch ($command) {");
        sb.AppendLine("        'search' {");
        sb.AppendLine("            $opts = @('-t', '--tenant-id', '-m', '--make', '--model', '--year-min', '--year-max',");
        sb.AppendLine("                      '--price-min', '--price-max', '--mileage-max', '--condition', '--body-style',");
        sb.AppendLine("                      '--drivetrain', '-f', '--format', '-i', '--interactive', '-v', '-q')");
        sb.AppendLine("        }");
        sb.AppendLine("        'scrape' {");
        sb.AppendLine("            $opts = @('-t', '--tenant-id', '-s', '--site', '-m', '--make', '--model',");
        sb.AppendLine("                      '-p', '--postal-code', '-r', '--radius', '--max-pages', '--delay')");
        sb.AppendLine("        }");
        sb.AppendLine("        'backup' {");
        sb.AppendLine("            $opts = @('-o', '--output', '-l', '--list', '--cleanup', '--retention')");
        sb.AppendLine("        }");
        sb.AppendLine("        'restore' {");
        sb.AppendLine("            $opts = @('-y', '--yes')");
        sb.AppendLine("        }");
        sb.AppendLine("        'import' {");
        sb.AppendLine("            $opts = @('-t', '--tenant-id', '-f', '--format', '--dry-run')");
        sb.AppendLine("        }");
        sb.AppendLine("        'config' {");
        sb.AppendLine("            if ($elements.Count -le 3) {");
        sb.AppendLine("                $opts = @('list', 'get', 'set', 'reset', 'path')");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        'profile' {");
        sb.AppendLine("            if ($elements.Count -le 3) {");
        sb.AppendLine("                $opts = @('save', 'load', 'list', 'delete')");
        sb.AppendLine("            } else {");
        sb.AppendLine("                $opts = @('-t', '--tenant-id', '-d', '--description')");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        'completion' {");
        sb.AppendLine("            if ($wordToComplete -eq '' -and $elements[-1].Value -in @('-s', '--shell')) {");
        sb.AppendLine("                $opts = @('bash', 'zsh', 'powershell', 'fish')");
        sb.AppendLine("            } else {");
        sb.AppendLine("                $opts = @('-s', '--shell', '-o', '--output')");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        default {");
        sb.AppendLine("            $opts = @('-t', '--tenant-id', '-v', '-q')");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    $opts | Where-Object { $_ -like \"$wordToComplete*\" } | ForEach-Object {");
        sb.AppendLine("        [System.Management.Automation.CompletionResult]::new($_, $_, 'ParameterValue', $_)");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static string GenerateFishCompletion()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Fish completion script for {AppName}");
        sb.AppendLine($"# Add this to ~/.config/fish/completions/{AppName}.fish");
        sb.AppendLine();
        sb.AppendLine($"# Disable file completion for {AppName}");
        sb.AppendLine($"complete -c {AppName} -f");
        sb.AppendLine();
        sb.AppendLine("# Commands");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'search' -d 'Search for car listings in the database'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'scrape' -d 'Scrape car listings from automotive websites'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'list' -d 'List saved car listings from the database'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'export' -d 'Export car listings to JSON or CSV files'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'config' -d 'Manage application configuration settings'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'profile' -d 'Manage search profiles'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'status' -d 'Display system status and statistics'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'stats' -d 'Display market statistics and analysis'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'show' -d 'Display detailed information about a specific listing'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'import' -d 'Import car listings from CSV or JSON files'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'backup' -d 'Create database backups'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'restore' -d 'Restore database from a backup file'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'compare' -d 'Compare car listings'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'review' -d 'Review queue management'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'alert' -d 'Alert management'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'watch' -d 'Watch list management'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_use_subcommand' -a 'completion' -d 'Generate shell completion scripts'");
        sb.AppendLine();
        sb.AppendLine("# Search command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -s t -l tenant-id -d 'Tenant ID' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -s m -l make -d 'Vehicle make' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -l model -d 'Vehicle model' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -l year-min -d 'Minimum year' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -l year-max -d 'Maximum year' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -l price-min -d 'Minimum price' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -l price-max -d 'Maximum price' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -l condition -d 'Vehicle condition' -ra 'New Used Certified'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -s f -l format -d 'Output format' -ra 'table json csv'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from search' -s i -l interactive -d 'Interactive mode'");
        sb.AppendLine();
        sb.AppendLine("# Scrape command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from scrape' -s t -l tenant-id -d 'Tenant ID' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from scrape' -s s -l site -d 'Website to scrape' -ra 'autotrader kijiji cargurus all'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from scrape' -s m -l make -d 'Vehicle make' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from scrape' -s p -l postal-code -d 'Postal code' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from scrape' -s r -l radius -d 'Search radius' -r");
        sb.AppendLine();
        sb.AppendLine("# Backup command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from backup' -s o -l output -d 'Output file path' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from backup' -s l -l list -d 'List backups'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from backup' -l cleanup -d 'Clean up old backups'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from backup' -l retention -d 'Number of backups to keep' -r");
        sb.AppendLine();
        sb.AppendLine("# Restore command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from restore' -s y -l yes -d 'Skip confirmation'");
        sb.AppendLine();
        sb.AppendLine("# Import command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from import' -s t -l tenant-id -d 'Tenant ID' -r");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from import' -s f -l format -d 'File format' -ra 'csv json'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from import' -l dry-run -d 'Preview import without saving'");
        sb.AppendLine();
        sb.AppendLine("# Config command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from config' -a 'list get set reset path' -d 'Config operation'");
        sb.AppendLine();
        sb.AppendLine("# Profile command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from profile' -a 'save load list delete' -d 'Profile action'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from profile' -s t -l tenant-id -d 'Tenant ID' -r");
        sb.AppendLine();
        sb.AppendLine("# Completion command options");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from completion' -s s -l shell -d 'Shell type' -ra 'bash zsh powershell fish'");
        sb.AppendLine($"complete -c {AppName} -n '__fish_seen_subcommand_from completion' -s o -l output -d 'Output file' -r");
        sb.AppendLine();
        sb.AppendLine("# Global options");
        sb.AppendLine($"complete -c {AppName} -s v -l verbose -d 'Verbose output'");
        sb.AppendLine($"complete -c {AppName} -s q -l quiet -d 'Quiet mode'");

        return sb.ToString();
    }

    private static void ShowInstallationInstructions(string shell, string filePath)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Installation Instructions:[/]");

        switch (shell)
        {
            case "bash":
                AnsiConsole.MarkupLine("[dim]Add to your ~/.bashrc:[/]");
                AnsiConsole.MarkupLine($"  source {filePath}");
                break;
            case "zsh":
                AnsiConsole.MarkupLine("[dim]Option 1: Add to your ~/.zshrc:[/]");
                AnsiConsole.MarkupLine($"  source {filePath}");
                AnsiConsole.MarkupLine("[dim]Option 2: Copy to your fpath:[/]");
                AnsiConsole.MarkupLine($"  cp {filePath} ~/.zsh/completions/_car-search");
                break;
            case "powershell":
            case "pwsh":
                AnsiConsole.MarkupLine("[dim]Add to your PowerShell profile ($PROFILE):[/]");
                AnsiConsole.MarkupLine($"  . {filePath}");
                break;
            case "fish":
                AnsiConsole.MarkupLine("[dim]Copy to Fish completions directory:[/]");
                AnsiConsole.MarkupLine($"  cp {filePath} ~/.config/fish/completions/car-search.fish");
                break;
        }
    }

    public class Settings : CommandSettings
    {
        [CommandOption("-s|--shell <SHELL>")]
        [Description("Shell to generate completion for (bash, zsh, powershell, fish). Auto-detected if not specified.")]
        public string? Shell { get; set; }

        [CommandOption("-o|--output <PATH>")]
        [Description("Output file path. If not specified, outputs to stdout.")]
        public string? OutputPath { get; set; }
    }
}
