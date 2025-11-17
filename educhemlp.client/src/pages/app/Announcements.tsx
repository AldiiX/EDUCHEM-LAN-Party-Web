import { AppLayout } from "./AppLayout.tsx";
import { useEditor, EditorContent } from "@tiptap/react";
import StarterKit from "@tiptap/starter-kit";
import Underline from "@tiptap/extension-underline";
import TextAlign from "@tiptap/extension-text-align";
import { TextStyle, Color, FontSize } from "@tiptap/extension-text-style";
import Image from "@tiptap/extension-image";
import FileHandler from "@tiptap/extension-file-handler";
import { useRef, useState } from "react";
import styles from "./Announcements.module.scss";

// upload na backend, vrat url kde bude obrazek dostupny
async function uploadImage(file: File): Promise<string> {
    // TODO: upravit si endpoint a odpoved podle api
    const form = new FormData();
    form.append("file", file);
    const res = await fetch("/api/uploads/images", { method: "POST", body: form });
    if(!res.ok) { throw new Error("upload failed"); }
    const data = await res.json();
    return data.url || data.src; // ocekavany klic s verejnou url
}

function RichEditor({ className }: { className?: string }) {
    const [color, setColor] = useState("#333333");
    const sizes = ["12px","14px","16px","18px","24px","30px","36px","48px"];
    const fileInputRef = useRef<HTMLInputElement>(null);

    const editor = useEditor({
        extensions: [
            StarterKit,
            Underline,
            TextStyle,
            Color,
            FontSize,
            TextAlign.configure({ types: ["heading", "paragraph"] }),
            Image.configure({
                //allowBase64: true, // volitelne, pokud chces docasne data: url
            }),

            /*FileHandler.configure({
              allowedMimeTypes: ["image/jpeg","image/png","image/webp","image/gif","image/svg+xml"],
              onDrop: async (editor, files, pos) => {
                for(const file of files) {
                  const url = await uploadImage(file);
                  editor
                    .chain()
                    .focus()
                    .insertContentAt(pos, { type: "image", attrs: { src: url, alt: file.name } })
                    .run();
                }
              },
              onPaste: async (editor, files) => {
                for(const file of files) {
                  const url = await uploadImage(file);
                  editor
                    .chain()
                    .focus()
                    .setImage({ src: url, alt: file.name })
                    .run();
                }
              },
            }),*/
        ],
        content: "",
        editorProps: {
            attributes: { class: `tiptap ${styles.editor}` }, // pro scss moduly
        },
    });

    if(!editor) { return null; }

    // barva
    const applyColor = () => editor.chain().focus().setColor(color).run();
    const clearColor = () => editor.chain().focus().unsetColor().run();

    // manualni nahrani souboru pres tlacitko
    const onPickImage = () => fileInputRef.current?.click();
    const onFileChange: React.ChangeEventHandler<HTMLInputElement> = async (e) => {
        const file = e.target.files?.[0];
        if(!file) return;
        const url = await uploadImage(file);
        editor.chain().focus().setImage({ src: url, alt: file.name }).run();
        e.target.value = "";
    };

    return (
        <div className={`${styles["rich-editor"]} ${className ?? ""}`}>
            <div className={styles["rich-editor__toolbar"]}>
                <div className={styles["group"]}>
                    <button className={`button-tertiary-rich ${editor.isActive("bold") ? "is-active" : ""}`} onClick={() => editor.chain().focus().toggleBold().run()}>Tučné</button>
                    <button className={`button-tertiary-rich ${editor.isActive("italic") ? "is-active" : ""}`} onClick={() => editor.chain().focus().toggleItalic().run()}>Kurzíva</button>
                    <button className={`button-tertiary-rich ${editor.isActive("strike") ? "is-active" : ""}`} onClick={() => editor.chain().focus().toggleStrike().run()}>Přeškrtnuté</button>
                    <button className={`button-tertiary-rich ${editor.isActive("underline") ? "is-active" : ""}`} onClick={() => editor.chain().focus().toggleUnderline().run()}>Podtržené</button>
                </div>

                <div className={`${styles["group"]} ${styles["color-field"]}`}>
                    <span>Barva:</span>
                    <input type="color" value={color} onChange={(e) => setColor(e.target.value)} />
                    <button className="button-tertiary-rich" onClick={applyColor}>Použít</button>
                    <button className="button-tertiary-rich" onClick={clearColor}>Vymazat</button>
                </div>

                <div className={styles["group"]}>
                    <span>Zarovnání:</span>
                    <button className={`button-tertiary-rich ${editor.isActive({ textAlign: "left" }) ? "is-active" : ""}`} onClick={() => editor.chain().focus().setTextAlign("left").run()}>Vlevo</button>
                    <button className={`button-tertiary-rich ${editor.isActive({ textAlign: "center" }) ? "is-active" : ""}`} onClick={() => editor.chain().focus().setTextAlign("center").run()}>Na střed</button>
                    <button className={`button-tertiary-rich ${editor.isActive({ textAlign: "right" }) ? "is-active" : ""}`} onClick={() => editor.chain().focus().setTextAlign("right").run()}>Vpravo</button>
                    <button className={`button-tertiary-rich ${editor.isActive({ textAlign: "justify" }) ? "is-active" : ""}`} onClick={() => editor.chain().focus().setTextAlign("justify").run()}>Do bloku</button>
                </div>

                <div className={styles["group"]}>
                    <span>Velikost fontu:</span>
                    {["12px","14px","16px","18px","24px","30px","36px","48px"].map((size) => (
                        <button key={size} className={`button-tertiary-rich ${editor.isActive("textStyle", { fontSize: size }) ? "is-active" : ""}`} onClick={() => editor.chain().focus().setFontSize(size).run()}>
                            {parseInt(size, 10)}
                        </button>
                    ))}
                    <button className="button-tertiary-rich" onClick={() => editor.chain().focus().unsetFontSize().run()}>Reset</button>
                </div>

                {/* tlacitko pro nahrani obrazku */}
                {/*<div className={styles["group"]}>*/}
                {/*  <button className="button-primary" onClick={onPickImage}>Nahrát obrázek</button>*/}
                {/*  <input ref={fileInputRef} type="file" accept="image/*" onChange={onFileChange} style={{ display: "none" }} />*/}
                {/*</div>*/}
            </div>

            <div className={styles["rich-editor__content"]}>
                <EditorContent editor={editor} />
            </div>
        </div>
    );
}

export const Announcements = () => (
    <AppLayout>
        <h1>Oznámení</h1>
        {/*<RichEditor className={styles.richEditor} />*/}
        <p style={{ marginTop: 16, opacity: 0.25 }}>Bude implementováno ve verzi <a href={'https://github.com/AldiiX/EDUCHEM-LAN-Party-Web/milestone/2'} target='_blank'>3.5.0</a> (na další LAN Party)</p>
    </AppLayout>
);

export default Announcements;