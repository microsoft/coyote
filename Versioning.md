# Guidance on Coyote versioning

#### v1.0.0

The Coyote framework versioning follows [Semantic Versioning 2.0.0](https://semver.org/) and has the pattern of MAJOR.MINOR.PATCH.

1. MAJOR version when you make incompatible API changes,
2. MINOR version when you add functionality in a backwards compatible manner, and
3. PATCH version when you make backwards compatible bug fixes.

We adopt everything listed in [Semantic Versioning 2.0.0](https://semver.org/)
but to summarize the major points here:

**Incrementing the MAJOR version number** should be done when:

- a major new feature is added to the Coyote framework or tools and is showcased by a new tutorial demonstrating this feature
- a breaking change has been made to Coyote API or serialization format, or  tool command line arguments.


**Incrementing the MINOR version number** should be done when:
- new features are added to the Coyote API or tools that are backwards compatible
**Incrementing the PATCH version number** should be done when

- anything else changes in the the Coyote framework.
Not all changes to the repository warrant a version change. For example,

- test code changes
- documentation only fixing typos and grammar
- automation script updates
- reformatting of code for styling changes
## Process

Developers maintain `History.md` with each checkin, adding bullet points to the top version number listed with an asterix. For example, it might look like this:

```
## v2.4.5*
- Fix some concurrency bugs in the framis checker.
```

The asterix means this version has not yet reached the master branch on GitHub. Each developer
modifies this the new version number in `History.md` according to the above rules. For example, one
developer might fix a bug and bump the **patch** version number from "v2.4.5*" to "v2.4.6*". Another
might then add a big new feature that warrants a major version change, so they will change the
top record in `History.md` from "v2.4.6*" to "v3.0.0*". All other changes made from there will leave
the number at "v3.0.0" until these bits are pushed to the master branch in GitHub.

When all this is pushed to GitHub via a Pull Request, `Common\version.props` and
`Scripts\NuGet\Coyote.nuspec` are updated with the new version number listed in `History.md` and the
asterix is removed, indicating this version number is now locked. The next person to change the repo will then start a new version number record in `History.md` and add the asterix to indicate to everyone else
that this new number is not yet locked.

The contents in `History.md` is then copied to the GitHub release page, which makes it easy to do
a release. There is no need to go searching through the change log to come up with the right
release summary. Code reviews are used to ensure folks remember to update this `History.md` file.
