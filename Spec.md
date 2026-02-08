# ZenHanConverter ソースコード詳細仕様書

## 概要
- 全角/半角変換を中心とする文字列変換ユーティリティ群。
- CSV 定義からコード生成された静的クラス群と、変換エントリ/変換リストを扱う汎用クラスで構成されます。
- `ConvertMethod` による複合変換 API で正規化から幅変換まで一括適用できます。

## アーキテクチャ概要
- 変換定義は `ConvertDefine.csv` → T4 (`Generated.tt`) で `Generated.cs` に自動生成され、カテゴリ別静的アクセサを提供。
- コアロジックは `ConvertPairs`/`EntryRecord` レコードで共通化し、キャッシュと不変コレクションでスレッドセーフに高速化。
- 複合メソッド群 (`ConvertMethod.cs`) は生成済みの `GroupOf` クラス経由で `ConvertPairs` を連結して高レベル API を構成。

## 主な型とメンバー
### `ZenHanConverter` (in `ConvertMethod.cs`)
- パブリックな静的ユーティリティクラス。
- アプリケーションが直接利用する高レベル API (`ToHan`, `ToZen...` 等) を提供。

### `Generated.cs` 内の型
- `Define`: 全定義リスト (`AllList`) などを保持。
- `GroupOf`: `Ascii`, `Kana` などのカテゴリ別グループへのアクセサを提供。
- `GroupOf_Ascii` / `GroupOf_Kana`: 各カテゴリ内の変換マップ (`ToHanMap`, `ToZenMap` 等) をプロパティとして公開。
- `NameOf`: 定義されている個々のエントリ名へのアクセサ。

### `ConvertMethod` に定義された複合 API (public static)
- `ToNormalize(string text)`: 空白・ダッシュ・分離カナを正規化。
- `ToHan(string text)`: 数字/英字/記号/カナを半角へ統一。
- `ToZenWithKatakana(string text)`: 数字/英字/記号/半角カナを全角カタカナで統一。
- `ToZenWithHiragana(string text)`: 数字/英字/記号/半角カナを全角ひらがなで統一。
- `ToHanOnlyAscii(string text)`: 数字/英字/記号のみ半角化。
- `ToZenOnlyAscii(string text)`: 数字/英字/記号のみ全角化。
- `ToHanOnlyKana(string text)`: ひらがな/カタカナと関連記号を半角化。
- `ToHanOnlyKatakana(string text)`: カタカナのみ半角化。
- `ToZenKatakanaOnlyKatakana(string text)`: 半角カナを全角カタカナへ統一。
- `ToZenKatakanaOnlyKana(string text)`: ひらがなと半角カナを全角カタカナへ統一。
- `ToZenHiraganaOnlyKana(string text)`: カタカナ（全角/半角）をひらがなへ統一。
- `ToUpperCase(string text)`: 全角/半角の英字を大文字へ。
- `ToLowerCase(string text)`: 全角/半角の英字を小文字へ。
- `ConvertTabToSpace(string text)`: タブを半角スペースへ。
- `ConvertBackslashToHanYen(string text)`: バックスラッシュを半角円記号へ。

### `ConvertPairs` 系
- 役割: `(Source, Target)` ペアを保持し、Regex を用いた置換や連鎖/統合を提供。
- コンストラクタ: `IEnumerable<(string Source, string Target)>` 版と `params` で複数集合を平坦化する版。
- 列挙: `IEnumerable<(string, string)>` を実装し、`GetEnumerator` でペア列挙を提供。
- 連鎖/統合: `Chain(ConvertPairs, bool includeUnmatchedFirst, bool includeUnmatchedSecond)` で Target と Source を突合し連鎖。`Chain()` は未マッチ切り捨て、`ChainMerge()` は未マッチも保持。
- 変換実行: `Convert(string text)` はキャッシュ済み Regex/辞書で置換。Regex 生成不可時は空文字列を返却。null 入力は空文字扱い。
- 静的生成: `FromForward` / `FromInverse` / `FromFunc` 等で `EntryRecord` 一覧や遅延ロード関数からインスタンス化。
- 内部フィールド: `_cacheRegexMapDictionary` (`ConcurrentDictionary<ConvertPairs, (Regex regex, Dictionary<string, string> map)>`) で Regex/辞書をキャッシュ。

### `EntryRecord`
- プロパティ: `Category`, `Group`, `SubGroup`, `Forward`, `Inverse`, `Source`, `Target`, `Name`, `Summary`。
- コンストラクタ: 文字列9要素版とタプル版。`U+XXXX` をデコードし、null を空文字へフォールバック。
- 暗黙変換: `(string, ...)` タプルから `EntryRecord` へ。
- デコンストラクタ: `(Source, Target)` 版と全プロパティ版を提供。
- 静的メソッド: `GetEntryList(key)` で階層キー (`ZenHanConverter|...`) に応じた一覧を取得・キャッシュ。
- 内部フィールド: `_cacheEntryListMap` (`ConcurrentDictionary<string, ImmutableList<EntryRecord>>`) でフィルタ結果をキャッシュ。

## 定義済みエントリ一覧 (GroupOf / NameOf)
`GroupOf` クラスおよび `NameOf` クラスでアクセス可能な定義一覧です。
`GroupOf.{カテゴリ}.{グループ}` で `ConvertPairs` を、`NameOf.{カテゴリ}.{定義名}` で個別の定義を取得できます。

| カテゴリ (`GroupOf`) | グループ (`GroupOf`プロパティ) / 定義名 (`NameOf`プロパティ) |
| --- | --- |
| `Ascii`<br/>(英数字・記号) | ***`Numeric`*** (数字 グループ)<br/>`n0`: ０<br/>`n1`: １<br/>`n2`: ２<br/>`n3`: ３<br/>`n4`: ４<br/>`n5`: ５<br/>`n6`: ６<br/>`n7`: ７<br/>`n8`: ８<br/>`n9`: ９ |
| `Ascii`<br/>(英数字・記号) | ***`Alphabet`*** (英字 グループ)<br/>※全角/半角、大文字/小文字の全パターンを網羅<br/>`A`: Ａ<br/>`B`: Ｂ<br/>`C`: Ｃ<br/>`D`: Ｄ<br/>`E`: Ｅ<br/>`F`: Ｆ<br/>`G`: Ｇ<br/>`H`: Ｈ<br/>`I`: Ｉ<br/>`J`: Ｊ<br/>`K`: Ｋ<br/>`L`: Ｌ<br/>`M`: Ｍ<br/>`N`: Ｎ<br/>`O`: Ｏ<br/>`P`: Ｐ<br/>`Q`: Ｑ<br/>`R`: Ｒ<br/>`S`: Ｓ<br/>`T`: Ｔ<br/>`U`: Ｕ<br/>`V`: Ｖ<br/>`W`: Ｗ<br/>`X`: Ｘ<br/>`Y`: Ｙ<br/>`Z`: Ｚ |
| `Ascii`<br/>(英数字・記号) | ***`Symbol`*** (記号 グループ)<br/>`ParenthesisLeft`: 丸括弧 左<br/>`ParenthesisRight`: 丸括弧 右<br/>`SquareBracketLeft`: 角括弧 左<br/>`SquareBracketRight`: 角括弧 右<br/>`CurlyBracketLeft`: 波括弧 左<br/>`CurlyBracketRight`: 波括弧 右<br/>`DoubleQuote`: ダブルクォート<br/>`SingleQuote`: シングルクォート<br/>`Backquote`: バッククォート<br/>`Comma`: カンマ<br/>`Period`: ピリオド<br/>`Colon`: コロン<br/>`Semicolon`: セミコロン<br/>`LessThan`: 不等号 小<br/>`GreaterThan`: 不等号 大<br/>`Equal`: 等号<br/>`Plus`: プラス<br/>`HyphenMinus`: ハイフン<br/>`Question`: はてな<br/>`Exclamation`: 感嘆符<br/>`Sharp`: シャープ<br/>`Dollar`: ドル<br/>`Percent`: パーセント<br/>`Ampersand`: アンパサンド<br/>`Asterisk`: アスタリスク<br/>`At`: アットマーク<br/>`Caret`: キャレット<br/>`UnderBar`: アンダーバー<br/>`VerticalBar`: 縦棒<br/>`Tilde`: チルダ<br/>`Slash`: スラッシュ<br/>`Bslash`: 逆斜線 (U+005C)<br/>`Yen`: 円 (U+00A5)<br/>`Space`: スペース (U+3000 / U+0020) |
| `Ascii`<br/>(英数字・記号) | ***`Replace`*** (置換・正規化 グループ)<br/>`Space`: 各種特殊空白を U+0020 へ<br/>`Hyphen`: 各種ダッシュ/マイナスを U+002D へ<br/>`Tab`: タブ (U+0009) を U+0020 へ<br/>`Yen`: 全角/半角円記号 ⇔ バックスラッシュ<br/>`Bslash`: バックスラッシュ ⇔ 円記号 |
| `Kana`<br/>(かな) | ***`Kata`*** (カタカナ グループ)<br/>※清音/濁音/半濁音/小書き、結合文字含む<br/>`A`: ア<br/>`I`: イ<br/>`U`: ウ<br/>`E`: エ<br/>`O`: オ<br/>`KA`: カ<br/>`KI`: キ<br/>`KU`: ク<br/>`KE`: ケ<br/>`KO`: コ<br/>`SA`: サ<br/>`SHI`: シ<br/>`SU`: ス<br/>`SE`: セ<br/>`SO`: ソ<br/>`TA`: タ<br/>`CHI`: チ<br/>`TSU`: ツ<br/>`TE`: テ<br/>`TO`: ト<br/>`NA`: ナ<br/>`NI`: ニ<br/>`NU`: ヌ<br/>`NE`: ネ<br/>`NO`: ノ<br/>`HA`: ハ<br/>`HI`: ヒ<br/>`FU`: フ<br/>`HE`: ヘ<br/>`HO`: ホ<br/>`MA`: マ<br/>`MI`: ミ<br/>`MU`: ム<br/>`ME`: メ<br/>`MO`: モ<br/>`YA`: ヤ<br/>`YU`: ユ<br/>`YO`: ヨ<br/>`RA`: ラ<br/>`RI`: リ<br/>`RU`: ル<br/>`RE`: レ<br/>`RO`: ロ<br/>`WA`: ワ<br/>`WO`: ヲ<br/>`N`: ン<br/>`GA`～`PO`: 濁音・半濁音 (ガ, パ 等)<br/>`VU`: ヴ |
| `Kana`<br/>(かな) | ***`Hira`*** (ひらがな グループ)<br/>※カタカナと同等のキー名でひらがな定義を提供<br/>`A`: あ<br/>`I`: い<br/>... (カタカナと同様の定義名) |
| `Kana`<br/>(かな) | ***`Symbol`*** (かな記号 グループ)<br/>`Voice`: ゛ (濁点)<br/>`SemiVoice`: ゜ (半濁点)<br/>`Prolong`: ー (長音)<br/>`MiddleDot`: ・ (中点)<br/>`LeftCornerBracket`: 「<br/>`RightCornerBracket`: 」<br/>`Period`: 。 (句点)<br/>`Comma`: 、 (読点) |
| `Kana`<br/>(かな) | ***`Case`*** (かな 大小/種別変換 グループ)<br/>※カタカナ⇔ひらがな変換用 (定義名は `Kata`/`Hira` と共通) |

## 変換定義のカバレッジ
- 数値: `０`～`９` ↔ `0`～`9`。
- 英字: 全角大文字/小文字と半角大文字/小文字を相互変換。ケース変換対応。
- 記号: 括弧、クォート、区切り記号、算術/比較記号、`￥`記号等。
- カタカナ/ひらがな: 清音/濁音/半濁音/小書き/長音/中点/句読点/カギ括弧を全角/半角で相互変換。
- 分離カナ合成: `カ`+`゛` などの分離形を合成ルール (`KataCompose`) で正規化。
- フリンジ空白・ダッシュ: ノーブレークスペースや各種ダッシュを正規化 (`FringeCase`)。

## 内部実装ポリシー
- 文字列比較は `StringComparer.Ordinal` を原則使用。
- Regex 生成不可時は変換を行わず空文字を返し、例外を回避。
- 重複キーは「先勝ち」で決定性を確保。
- 不変コレクションによる共有で副作用を排除し、スレッドセーフを維持。
- `ConvertPairs.Chain` で第2段を Source ごとにグルーピングし O(1) 判定で連結。

## パフォーマンス向上施策
- **Regex/辞書キャッシュ**: `ConcurrentDictionary` でコンパイル済み Regex とマッピングをキャッシュ。
- **先勝ち辞書化**: 同一 Source を一度だけ登録し、無駄な上書きと処理時間を防止。
- **Unicode デコードの先行**: `EntryRecord` 生成時に `U+XXXX` 表記をデコードし、実行時オーバーヘッドを削減。
- **null/空の早期スキップ**: 変換不要ケースを早期 return。
- **不変リストのスライス展開**: `[..]` 構文による効率的なリスト構築。
- **連鎖処理の辞書化**: `ConvertPairs` 連鎖時に第2段を辞書化 (`GroupBy` + `ToDictionary`) し、結合の線形走査を削減。

## 注意事項
- 生成物 (`Generated.cs`) は CSV 変更時に再生成が必要。
- 新規カテゴリや種別を追加する場合、CSV・T4 テンプレートの両方を更新し、テストを拡張する。
- 変換定義の優先度は定義順に依存するため、追加順序に留意。
