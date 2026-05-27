# Manual Test Cases

1. Open Visual Studio 2022 17.14+ or Visual Studio 2026.
2. Install or debug the VSIX in the Experimental Instance.
3. Open an ASP.NET Core solution.
4. Confirm the extension loads without error.
5. Confirm ActivityLog reports the number of icons discovered from `assets/icons/folders`.
6. Confirm ActivityLog reports the number of icons discovered from `assets/icons/foldersOpen`.
7. Create common folders:
   - Controllers
   - Services
   - Repositories
   - Helpers
   - Middleware
   - Models
   - DTOs
   - Data
   - Migrations
   - wwwroot
   - Tests
   - Views
   - ViewModels
   - Components
   - Docker
   - GitHub
   - React
   - TypeScript
8. Verify each folder resolves only when the target SVG exists in `assets/icons/folders`.
9. Verify folders with no matching SVG keep the default Visual Studio folder icon.
10. Rename `Controllers` to `UnknownFolder`.
11. Verify `UnknownFolder` keeps the default Visual Studio folder icon.
12. Rename `UnknownFolder` back to `Controllers`.
13. Verify resolver behavior remains stable.
14. Test nested folders.
15. Test project reload.
16. Test solution reopen.
17. Disable the extension from Tools > Options > Material Folder Icons > General.
18. Verify initialization exits early and default behavior remains.
19. Temporarily rename one SVG file in an Experimental copy of the installed extension assets.
20. Verify Visual Studio does not crash and ActivityLog reports warnings.
21. With the same solution open, install or uninstall another VSIX extension, then restart Visual Studio when prompted.
22. Reopen the solution and verify previously mapped folders still show Material folder icons.
23. Create and rename a mapped folder after the extension change, for example `Controllers` -> `Services`.
24. Verify ActivityLog contains `hierarchy moniker fallback service is active` and the folder icons remain stable after Solution Explorer or another extension invalidates icons.
25. Run Visual Studio Installer Modify for an unrelated workload or component, then open the same solution again and repeat the mapped-folder verification.

Note: If ActivityLog says mappings were resolved but the project hierarchy did not accept custom icon properties, the tested project system is rejecting external hierarchy icon updates. In that case the extension is loading and resolving correctly, but Visual Studio is keeping its default folder icons.
