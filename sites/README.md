# Multi-Site Angular Solution

This folder contains all Angular sites for your solution. 

## How to add a new site
1. To add a new site, clone the client1 folder as a blueprint:
	- Run `cp -R client1 <new-site-name>` inside the sites/ folder.
	- Update the new site's package.json, angular.json, and branding as needed.
	- Install dependencies: `cd <new-site-name> && npm install`.
2. Alternatively, run `ng new <site-name>` for a fresh Angular app.
3. Each site should be a full Angular app in its own subfolder (e.g., `sites/client1/`, `sites/client2/`).
4. Manage each site's dependencies and configuration independently.
5. Use VS Code tasks or scripts to build/serve individual or all sites as needed.

---

This structure allows you to easily add, remove, or update sites without affecting others.