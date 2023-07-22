# Unit testing

## Web Role unit tests

In order to run .NET unit tests:

1. Test -> Test Settings -> Default Processor Architecture - select `x64`.
1. Test -> Windows -> Test Explorer - to show `Test Explorer` window, where you
   need to pick Run All tests command.

## Frontend unit tests

Frontend unit tests run through Karma test runner using Chrome headless browser.
Unit test files have the extension `spec.ts`.

### Debugging

There are three gulp targets which help in debugging unit tests:

1. `gulp test:unit` runs all the unit tests once and shows results on console.
1. `gulp test:unit:watch` watches for changes in js files, runs all tests and
   shows results in console.
1. `gulp test:unit:dev` watches for changes in js files, runs all
   tests and shows results in browser, also reloads browser automagically.
   Helpful for debugging by stepping through code.

Please remember to utilize the `f` Jasmine hook along with these gulp targets to
make iterative development faster. For instance, you can replace your top-level
`describe` block with `fdescribe` to only run that block while executing tests.

### Adding coverage

If your unit tests fail due to lack of coverage, you need to add more tests in
order to pass the coverage check. Do the following:

1. First inspect what exact coverage metric is failing. Is it lines, functions,
   branches or statements?
1. Run `gulp test:unit:coverage:show`. This will open up the detailed coverage
   report in browser.
1. Navigate through the components and see where you can add unit tests to bump
   up the metric. For example: if function coverage metric is failing, you
   should look to add unit test for a new function.
1. Write new unit tests, sometimes this could mean writing tests for a new or
   shared component.
1. Run `gulp test:unit:coverage` to ensure that you pass the coverage check.
1. If you fail, go back to step 3.

In the unlikely event where you absolutely need to bring down the coverage
thresholds temporarily, please add a comment in code about your reasoning behind
the change. This should only happen in extreme cases. The comment should look
like this:

```typescript
// TODO: Bring line coverage back up to 79 once 12345678 is complete.
// Moving it down to 78 because there is an earthquake.
lines: 78,
```
