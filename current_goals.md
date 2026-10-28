# Your Role: Task Master

## Your "Guiding Instructions":

	```		
		Hello! Thank you for your time we're so excited you're working for us! Happy happy!
		
		SUBAGENTs:
		 - Coding and General Development
		 - Critical Peer Review
		 - Research
		
		TASK MASTER:
			All tasks are DONE and WORKED ON by SUBAGENTS. You are simply their task master.
		
			Steps for each task:
			 1. A new SUBAGENT instance reports back to you a maximum 2 paragraphs of context history regarding the task.
			 2. A new SUBAGENT instance does the work, you include instructions from the task + research from Step 1 + any useful instructions you decide the subagent may need. 
			 3. A new SUBAGENT instance critically reviews the work done from Step 2. You decide if we must go back to Step 2 given Step 3 feedback.
			 4. If SUBAGENT Step 3 reports no found issues and task adherence is fine, you flip in "current_goals.md" "DONE? NO!" to "DONE? YES!", a new SUBAGENT instance writes the commit for the task, including current_goals.md, and pushes.
		 
			References to variables like X and Y automatically imply that these are to be exposed properties for developer tweaking.
		
		SKILLS TO KNOW: for you and/or subagents:
			box-agent-playtest
			sbox-live-play-agent-loop
			sbox-editor-mcp-driving

		SIGNAL SERVER CLI: I may not be available at my pc, if clarification questions time out once, pass on the message to signal cli.
	```	

---


# WORKING DATA


SESSION START? YES!

	
TASKS:

	Task 0: DONE? YES!
		Know that the sbox-public repo has a lot of hooks on git pull and such, but we don't want all that output to clutter context so decide a way to wait on git doing work without reading all its output.
		Note (Q5 answer, adopted): agent git runs use SBOX_SKIP_HOOKS=1 to bypass the SboxBuild artifact-restore hooks (post-checkout/post-merge/post-rewrite, synchronous, exit 0 even on failure), and run `dotnet run --project engine/Tools/SboxBuild/SboxBuild.csproj -- build --skip-native --skip-managed` explicitly when artifacts must refresh. Hook output never enters context; failures surface by grepping the captured log for "artifact restore failed". No wrapper script.

	Task 1: DONE? YES!
		Make the community branch a functioning fork of arcevalis-master and resolve any conflicts.
		Result: community hard-reset to arcevalis-master be664993d (15 setup commits dropped per Q1). Review CLEAN (reset byte-exact, no premature push, live tree untouched).

	Task 2: DONE? NO!
		merge fix/mcp-pipe-skip-hotload fixes into community branch and resolve any conflicts.

	Task 3: DONE? NO!
		rebase master onto community and resolve any conflicts.
		Clarified (Q3): rebase upstream master INTO community via rebase (git rebase upstream/master community).
		
NOTES:


CLARIFICATION Q&A:

Q1 (Task 1): How should community become a functioning fork of arcevalis-master (15 own commits, 407 behind)?
An: Hard reset community to arcevalis-master (drop the 15 setup commits).

Q2 (Task 2): Merge method for the fix commits?
An: Cherry-pick just the 2 fix commits (recommended, clean).

Q3 (Task 3): "rebase master onto community" - which direction?
An: "rebase upstream master INTO community, changes going INTO community via rebase."

Q4: Where should subagents do the branch surgery?
An: Temp clone, push the resulting branches to origin when done; your live tree untouched.

Q5 (Task 0): Wait-without-output mechanism?
An: Agent runs use SBOX_SKIP_HOOKS=1 to bypass hooks, and run SboxBuild explicitly only when needed.

Parent decisions (change if wrong): Q3 adopted as `git rebase upstream/master community` in the clone (community's non-upstream commits replayed onto upstream head 890a3ddd2; known conflict in engine/Sandbox.Tools/GameMode.cs from the earlier paused rebase). Q4: all surgery in the scratch clone; the live tree (on community at 02cdadb7d) is NEVER touched; its local community branch will diverge from origin/community after our force-pushes until the user resets it himself. Task 0 convention: SBOX_SKIP_HOOKS=1 for agent git runs; run SboxBuild build --skip-native --skip-managed explicitly when artifacts must refresh; no wrapper script. Push policy: origin = CSEliot fork; Tasks 1 and 3 force-push origin/community (fork default branch), Task 2 fast-forwards; current_goals.md rides in each task commit per the doc's step 4 (snapshot copied from the live tree into the clone). Carried over: port proof 2026-10-28 - 1537dded9 + a755e86c0 cherry-pick cleanly onto be664993d (0 conflicts, temp clone).

AVAILABLE? Yes, answered via clarify at PC (2026-10-28).


---


# IGNORE

/goal Update /Dev/sbox-public project state according to document current_goals.md. If "SESSION START?" is "NO!" then consider best clarifying questions, ask them, and record them below and after that update "SESSION START?" to "YES!"

Checklist:
Session start == ?
Task Done? No/Yes?
Notes and Clarification sections empty?
Update [project-name] == correct project name?
Skills to know, if any, are correct?
