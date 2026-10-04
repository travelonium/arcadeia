/*
 *  Copyright © 2024 Travelonium AB
 *
 *  This file is part of Arcadeia.
 *
 *  Arcadeia is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Affero General Public License as published
 *  by the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  Arcadeia is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Affero General Public License for more details.
 *
 *  You should have received a copy of the GNU Affero General Public License
 *  along with Arcadeia. If not, see <https://www.gnu.org/licenses/>.
 *
 */

import { useSelector } from 'react-redux';

const help = {
    scanner: ['Scan folders for new media, refresh indexed files or clean up orphaned previews. Changes save automatically and restart the scanner; enabled startup jobs may run again.', 'scanner'],
    mounts: ['Connect an SMB or NFS share to a folder inside Arcadeia. Use Apply to save edits and remount storage; Reset discards unapplied edits. Keep mount credentials private; they are stored in configuration.', 'network-storage'],
    transcoding: ['Choose how FFmpeg processes video and audio for browser playback. Changes save automatically and apply to new processing work; restart playback to try them.', 'playback-and-ffmpeg'],
    transcription: ['Make spoken words searchable with local speech recognition. Changes save automatically. Transcription runs in the background and can use significant CPU and disk space.', 'transcription'],
    logging: ['Control which messages appear in the application logs. Changes save automatically. More detailed logging can expose media paths and produce large logs.', 'logging'],
};

export default function SettingsHelp({ section }) {
    const readOnly = useSelector(state => state.settings.current?.Security?.Settings?.ReadOnly);
    const [text, anchor] = help[section];
    return (
        <div className="mb-3">
            <p className="text-muted mb-2">{text}</p>
            {readOnly && <p className="text-muted mb-2">Settings are read-only on this instance. An administrator must unlock configuration outside this screen.</p>}
            <a href={`https://www.arcadeia.org/docs/settings.html#${anchor}`} target="_blank" rel="noopener noreferrer">Read the settings guide <i className="bi bi-box-arrow-up-right" aria-hidden="true"></i></a>
        </div>
    );
}
