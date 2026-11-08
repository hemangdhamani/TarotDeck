# Offline tarot reading plan

Status: implemented on `feature/offline-tarot-readings`; local commit only, no PR to master.

## Start on a new feature branch

Before any reading-feature implementation, merge the current UI/card-back PR,
update the local master branch from origin/master, and create a new branch named
`feature/offline-tarot-readings` from that updated master. Do not implement this
feature on `fix/card-spread-direction` or directly on master.

## Scope and experience

Build a complete offline reading experience for cards selected manually from the
fan or through Draw Cards. Use the same interpretation engine for both methods.
No AI, model downloads, external APIs, accounts, network calls, or paid services.
Readings provide symbolic interpretation, reflection, and practical next steps;
possible directions are conditional rather than guaranteed predictions.

The first release supports the existing three-card reading. Additional draws form
new readings or explicitly selected clarifiers; they must not silently replace a
saved reading. Keep all existing shuffle, spread, draw, paging, and artwork behavior.

## Reading setup

Before drawing, let the user optionally open Settings → Reading setup. Reading and
artwork preferences share one Apply/Cancel pair; pending edits stay inactive until Apply:

- Spread: Situation / Advice / Outcome (default), Situation / Challenge / Advice, Past / Present / Possible
  direction, or Mind / Body / Spirit.
- Topic: General (default), Relationships, Career, or Personal growth.
- Focus: an optional topic-specific choice, such as making a decision, improving
  communication, understanding an obstacle, or choosing a next step.
- No free-form question or context input. Use only the structured spread, topic,
  and focus selectors to tailor readings.
- Upright cards only in the first release. The existing IsCardUp property controls
  face visibility, not reversal. Reversed readings require a separate future design.

Personalization comes exclusively from topic, focus, spread position, and actual
cards. Do not accept or infer intent from free-flow questions. Optional journal
notes are the user's reflections after a reading; they never enter the engine.

Keep setup compact and optional. Show position labels beside the drawn cards
without moving them or disrupting their symmetry. Read Cards is available once
three positions are filled. For existing drawn cards, offer to interpret the
current three-card page in its displayed left-to-right order; distinguish that
order from historical draw order and never silently switch between them.

## Reading presentation

Open results in a separate owned reading window, using the proven Settings/Credits
window approach. Keep the cards visible on the main screen and preserve their state.

Present the reading in this order:

1. A concise overall theme, based on explicit, reviewed combination rules.
2. Three position-specific interpretations with the card's name, thumbnail,
   orientation, keywords, and explanation of its role in this spread.
3. Connections: relevant shared themes, tensions, and progression between cards.
4. Two or three concrete, non-repetitive next steps tailored to topic and focus.
5. One or two reflection questions and space for the user's own interpretation.

Offer a short overview first and expandable detail. Avoid a wall of generic text,
repeated keywords, automatic positive/negative scores, exact dates, probability
claims, and deterministic yes/no outcomes. Challenging imagery should be explained
without literal predictions of death, illness, disaster, or another person's intentions.
Consequential decisions remain the user's; guidance should invite reflection.

For example, Career / Next step offers workplace prompts based on the selected
cards and structured focus, without any question text or natural-language parsing.

## Meaning library and deterministic engine

Create an original, reviewed content library covering all 78 canonical card IDs.
Keep it independent of filenames, artwork decks, and card backs. Start with shared
Rider-Waite-Smith-based meanings; do not invent deck-specific visual symbolism.
Do not copy contemporary guidebook descriptions without appropriate permission.

For each card record:

- Canonical ID, display name, suit, rank/arcana, and stable thematic tags.
- Core meaning, constructive expression, challenge, and reflection prompts.
- Upright and, when enabled, reviewed reversed interpretations.
- Spread-role guidance and topic-specific guidance, actions, and focus prompts.
- Content version, editorial notes, and any required source/license attribution.

Interpretation pipeline:

1. Validate a snapshot of three distinct card IDs, upright orientation, spread,
   topic, focus, artwork reference, and content version.
2. Resolve each card's reviewed meaning and position/topic/focus guidance.
3. Evaluate a small set of explicit combination rules over card tags and positions.
   Include relevant suit/arcana patterns only when they add useful explanation.
4. Rank and deduplicate applicable connections and actions. Limit output to the
   most relevant items and explain which cards support each connection.
5. Assemble edited, natural templates. Use a neutral per-card synthesis when no
   combination rule applies; never fabricate a specific interaction.

Use deterministic tie-breaking so identical inputs and content versions give the
same reading. Prefer clear, reviewed coverage over thousands of unreviewed card
pair combinations or random sentence assembly. Reversed meanings are not simply
the opposite of upright meanings.

## State, history, and privacy

- Reading analysis must never redraw, reshuffle, reorder, rotate, or move cards.
- Preserve immutable reading snapshots; changing a deck/back or paging must not
  rewrite an existing reading. Reinterpretation creates an explicit new revision.
- Save readings and reflection notes locally, with date, setup, card identities/orientations,
  interpretation, content version, and optional artwork identifiers.
- Keep historical text as originally generated when the meaning library changes.
- Provide save/reopen, user-controlled deletion, and plain-text export locally.
- No question input or question storage exists. Never upload journal notes.

## Delivery sequence

1. Branch and state audit: create the required feature branch; inspect selection,
   paging, orientation, and model identity. Define snapshots and spread positions.
2. Content foundation: establish schemas and editorial guidance; review a small
   sample covering Major Arcana and each suit before completing all 78 entries.
3. Core experience: setup, position labels, Read Cards, per-card meanings, and
   owned reading window. Both selection methods must use the same snapshot path.
4. Synthesis: add reviewed combination rules, topic/focus guidance, deduplication,
   useful actions, and reflection prompts. Complete content coverage before release.
5. Journal: local persistence, notes, reopen/delete/export, and version handling.
6. Verification and polish: validate content, behavior, readability, DPI, window
   positioning, and offline operation. Commit on this feature branch; do not create
   a PR to master for this task.

Later offline extensions, outside the first release: physical-card entry with
searchable names and duplicate prevention, explicit clarifier cards, custom spreads,
and larger layouts, including reversals. Background-color settings are a separate task.

## Acceptance criteria

- Complete, reviewed content for all 78 cards and every supported spread/topic;
  no missing IDs, invalid references, placeholders, or unsupported reversals.
- Identical structured inputs produce identical output. There is no free-form
  question field; journal notes do not affect interpretations.
- Manual fan selection and Draw Cards produce equivalent interpretations for the
  same explicit snapshot. Card order and position labels agree with the display.
- No changes to fan slots, drawn-card bounds, images/orientations, or draw order
  when opening, closing, saving, or rereading analysis.
- Cover incomplete spreads, repeated draws, paging, shuffle, back/front changes,
  local history round trips, content-version changes, and malformed saved data.
- Verify real rendered layouts at the user's DPI and smaller supported windows;
  no clipped cards, hidden actions, stale overlay pixels, or empty-placeholder borders.
- Review representative readings for relevance, repetition, tone, and consistency.
  Separate editorial quality checks from automated schema and engine tests.
- Debug and Release builds pass; demonstrate complete readings with HTTP requests
  blocked by the regression harness before considering the offline feature finished.

## Implemented delivery and validation

- Structured setup has four spreads, four topics, and topic-specific focus choices;
  there is no free-form question field. Journal notes remain separate from analysis.
- All 78 canonical cards have original upright meanings, challenges, actions and
  reflection prompts, combined with explicit position/topic/focus and pattern rules.
- Analysis snapshots use the actual three displayed cards in left-to-right order.
  Position labels, partial-spread availability, paging and shuffle clearing are covered.
- Owned reading and journal windows provide an overview, individual card details,
  local notes, save/reopen/delete and UTF-8 text export. Historical reports are preserved.
- Regression checks cover 4,992 structured combinations, deterministic results,
  invalid identities/orientations/options, XML line endings, malformed/oversized files,
  path isolation, manual/automatic draws and preservation of card/fan state.
- HTTP requests are blocked throughout automated reading and integration tests;
  the runtime feature has no network client, AI dependency, or external service.
- Three additional full decks are bundled: Woodcut Marseille (CC0), Tarot de Viéville
  and Lequart Besançon (public-domain historical artwork). Each contains all 78
  canonical image IDs, source/license evidence and hashes. Historical trump/court
  mappings and Besançon's Juno/Jupiter equivalents are documented explicitly.
- Debug/Release output includes all artwork. Visual checks cover setup, overview,
  individual card thumbnails/details, saving, history, and visible footer actions.

Future editorial expansion can add further reviewed combination rules and historical
deck-specific reading systems; current readings consistently use the shared RWS library.
