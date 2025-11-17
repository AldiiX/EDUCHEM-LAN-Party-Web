import {AppLayout} from "./AppLayout.tsx";
import style from "./Problems.module.scss";
import {Button} from "../../components/buttons/Button.tsx";
import {ButtonType} from "../../components/buttons/ButtonProps.ts";

export const Problems = () => {
    return (
        <AppLayout>
            <main className={style.page}>
                <header className={style.header}>
                    <h1>Nahlásit problém</h1>
                    <p>
                        Pokud narazíš na chybu, máš nápad na vylepšení, nebo něco nefunguje tak, jak má, tady najdeš, jak
                        postupovat.
                    </p>
                </header>

                <div className={style.sections}>
                    <section className={style.section}>
                        <h2>Bug nebo nápad na vylepšení</h2>
                        <p>
                            Když objevíš chybu v aplikaci nebo tě napadne zlepšení, vytvoř prosím issue na GitHubu. Popiš co
                            nejpřesněji, co se stalo nebo co bys chtěl/a změnit.
                        </p>
                        <ul className={style.list}>
                            <li>stručný název problému nebo nápadu</li>
                            <li>co přesně nefunguje / co chceš vylepšit</li>
                            <li>jaký prohlížeč / zařízení používáš</li>
                        </ul>

                        <Button type={ButtonType.PRIMARY} text="Otevřít GitHub issue" className={style.btn} onClick={()=> window.open('https://github.com/AldiiX/EDUCHEM-LAN-Party-Web/issues/new')} />
                    </section>

                    <section className={style.section}>
                        <h2>Problémy s&nbsp;rezervací</h2>
                        <p>
                            Pokud máš problém přímo s&nbsp;rezervací (např. nejde ti potvrdit místo, nevidíš svůj počítač
                            na mapě, nesedí ti údaje apod.), nepiš prosím GitHub issue.
                        </p>
                        <p>
                            V takovém případě musíš kontaktovat správce LAN Party systému. Kontaktní údaje najdeš v
                            dokumentu{" "}
                            <a href="/info.pdf" target="_blank" rel="noreferrer" className={style.link}>
                                /info.pdf
                            </a>
                            .
                        </p>
                    </section>

                    <section className={style.section}>
                        <h2>Ostatní dotazy a věci kolem akce</h2>
                        <p>
                            Pokud řešíš cokoliv jiného (dotazy k akci, harmonogram, pravidla, techniku mimo systém
                            rezervací apod.), kontaktuj organizátory LAN Party.
                        </p>
                        <p>
                            Organizátoři jsou také uvedeni v dokumentu{" "}
                            <a href="/info.pdf" target="_blank" rel="noreferrer" className={style.link}>
                                /info.pdf
                            </a>
                            .
                        </p>
                        <p>
                            Můžeš také napsat na školní Discord server – organizátoři tam obvykle reagují nejrychleji.
                        </p>
                    </section>

                    <section className={style.noteSection}>
                        <p>
                            Čím více informací k problému nebo dotazu přidáš (screenshot, přesný postup, jak chybu
                            vyvolat), tím rychleji půjde věc vyřešit. Díky! 🧡
                        </p>
                    </section>
                </div>
            </main>
        </AppLayout>
    );
};

export default Problems;
