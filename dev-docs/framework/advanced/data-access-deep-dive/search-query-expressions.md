# 🥚 Search filter expressions

A search filter expression is a set of criteria used to define a subset of data to be retrieved from the database. In an expression, you can specify various conditions that the data must meet in order to be included in the search results. You can also use Boolean operators like `and` and `or` to combine multiple conditions and create more complex expressions.

In Smartstore, you can specify such expressions on data grid field level. Any textbox in a data grid filter form which contains a question mark icon can process expressions.

## Examples

<table><thead><tr><th width="360">Example</th><th>Result</th></tr></thead><tbody><tr><td>banana joe</td><td>Contains "banana" or contains "joe"</td></tr><tr><td>banana and !*.joe</td><td>Contains "banana" but does not match "*.joe"</td></tr><tr><td>banana and (!~"hello world" or !*jim)</td><td>Contains "banana", but does not contain "hello world" or does not end with "jim"</td></tr><tr><td>*Leather and !(Sofa Jacket*)</td><td>Ends with "Leather", but does not starts with "Sofa" or "Jacket"</td></tr><tr><td>(>=10 and &#x3C;=100) or 1 or >1000</td><td>Is between 10 and 100, or equals 1, or is greater than 1000</td></tr></tbody></table>

## Terms

Quoted search term (double or single), or unquoted search term without whitespaces. For example, unquoted _banana joe_ is the equivalent of _\~banana or \~joe_, whereas quoted _"banana joe"_ performs an exact term match. Supports wildcards (\* or ?). If wildcards are present, default operator is switched to "Equals" (=). Use "NotEquals" (!) to negate pattern.

## Groups

Multiple search words or phrases may be grouped in a fielded query by enclosing them in parenthesis. Any group can be negated with a preceding ! (exclamation mark), e.g. _!(banana and joe)_. Groups can be nested as often as required.

## Operators

<table data-header-hidden><thead><tr><th width="115"></th><th></th></tr></thead><tbody><tr><td>= <em>or</em> ==</td><td>Equals (default when omitted on numeric terms)</td></tr><tr><td>! <em>or</em> !=</td><td>Not equals</td></tr><tr><td>></td><td>Greater than</td></tr><tr><td>>=</td><td>Greater than or equal</td></tr><tr><td>&#x3C;</td><td>Less than</td></tr><tr><td>&#x3C;=</td><td>Less than or equal</td></tr><tr><td>~</td><td>Contains (default when omitted on string terms)</td></tr><tr><td>!~</td><td>Does not contain</td></tr><tr><td>and, or</td><td>Logical term combinators (case-insensitive). If omitted, "or" is default</td></tr></tbody></table>
