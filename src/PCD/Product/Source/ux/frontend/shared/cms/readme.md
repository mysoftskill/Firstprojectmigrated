This folder contains code that dynamically retrieves CMS content when user 
navigates between pages.

Currently this code is not being used, but it's ready to go, if needed. 

Frontend code does not care about which CMS backend is in use (ours was to be 
Compass).

When adding CMS-driven content, use `data-use-cms` attribute on elements with
static text, and rely on
`https://dev.manage.privacy.microsoft-int.com/?cms-xray=true` to show you
content that is hardcoded on the page.

```html
<!-- Before CMS is used. -->
<span data-use-cms>
    This is static string, it should be bound. When it's bound to CMS content, remove data-use-cms attribute.
</span>

<!-- After CMS is used. -->
<span>{{::$ctrl.content._s('stringFromCms')}}</span>
```

If you need to hardcode static content string in TypeScript code, expose it as a constant with `useCmsHere_` prefix, 
replace it with actual CMS content later on.

**NEVER** pass static strings from the web role.

```typescript
//  Before CMS is used.
const useCmsHere_SomeString = "Static content";

//  ...

public $onInit(): void {
    this.someMessage = useCmsHere_SomeString;
}

//  After CMS is used.

//  ...

public $onInit(): void {
    this.someMessage = this.content._s("stringFromCms");
}
```

Additional work that would integrate CMS with integration testing (i9n) is under
`users/supooja/i9n-offline-cms-prototype`. See [this PR for comments that must
be
addressed](https://microsoft.visualstudio.com/DefaultCollection/Universal%20Store/_git/MEE.Privacy.DataManagementUx.Svc/pullrequest/2310636?_a=overview).

CMS readme is under `users/supooja/cms-docs`. See [this PR for comments that
must be
addressed](https://microsoft.visualstudio.com/DefaultCollection/Universal%20Store/_git/MEE.Privacy.DataManagementUx.Svc/pullrequest/2333396?_a=overview).
