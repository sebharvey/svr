// Check for debug query string
const urlParams = new URLSearchParams(window.location.search);
const debugMode = urlParams.get('debug') === 'true';
    
// Show controls if debug mode is enabled
if (debugMode) {
    document.getElementById('controls').style.display = 'flex';
}

// Health check
async function checkHealth() {
    try {
        const response = await fetch('https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net/api/health');
        
        if (response.ok) {
            const healthData = await response.json();
            
            const statusElement = document.getElementById('systemStatus');
            const statusText = document.getElementById('systemStatusText');
            
            if (healthData.status === 'healthy') {
                statusElement.className = 'system-status online';
                statusText.textContent = 'System status: online';
            } else {
                statusElement.className = 'system-status offline';
                statusText.textContent = 'System status: unavailable';
            }
        } else {
            document.getElementById('systemStatus').className = 'system-status offline';
            document.getElementById('systemStatusText').textContent = 'System status: unavailable';
        }
    } catch (error) {
        console.error('Health check failed:', error);
        document.getElementById('systemStatus').className = 'system-status offline';
        document.getElementById('systemStatusText').textContent = 'System status: unavailable';
    }
}

let timetableData = null;

// Update timetable info display
function updateTimetableInfo() {
    const infoContainer = document.getElementById('timetableInfo');
    
    if (!timetableData) {
        infoContainer.classList.remove('visible');
        return;
    }
    
    let html = '';
    
    if (timetableData.name) {
        html += `<div class="timetable-name">${timetableData.name}</div>`;
    }
    
    if (timetableData.date) {
        html += `<div class="timetable-date">${timetableData.date}</div>`;
    }
    
    if (html) {
        infoContainer.innerHTML = html;
        infoContainer.classList.add('visible');
    } else {
        infoContainer.classList.remove('visible');
    }
}

// Fetch timetable from API
async function loadTimetable() {
    try {
        // Build API URL with optional debug parameter
        let apiUrl = 'https://svrliveapi-aaeydueba4b9aveb.uksouth-01.azurewebsites.net/api/timetable';
        if (debugMode !== null && debugMode) {
            apiUrl += `?debug=${debugMode}`;
        }
        
        const response = await fetch(apiUrl);
        
        // Handle 404 - no timetable available
        if (response.status === 404) {
            displayNoTimetableMessage();
            return;
        }
        
        // Handle other non-OK responses
        if (!response.ok) {
            throw new Error(`API returned status ${response.status}`);
        }
        
        timetableData = await response.json();
        
        // Update timetable info display
        updateTimetableInfo();
        
        // Update derived data
        updateTimetableData();
        
        // Initial render
        updateTimeDisplay();
        updateLiveButton();
        renderTracker();
        startAutoRefresh();
    } catch (error) {
        console.error('Error loading timetable:', error);
        displayErrorMessage(error.message);
    }
}

// Display message when no timetable is available
function displayNoTimetableMessage() {
    const trackerContainer = document.getElementById('trackerContainer');
    const statusContainer = document.getElementById('statusContainer');
    
    trackerContainer.innerHTML = `
        <div style="text-align: center; padding: 40px 20px; color: #888;">
            <div style="font-size: 48px; margin-bottom: 20px;">🚂</div>
            <h2 style="color: #e0e0e0; margin-bottom: 10px;">No Timetable Available</h2>
            <p>There is no scheduled timetable for today.</p>
            <p style="margin-top: 20px; font-size: 14px;">The Severn Valley Railway may not be operating services today.</p>
        </div>
    `;
    
    statusContainer.innerHTML = `
        <div style="text-align: center; color: #888; padding: 20px;">
            No train services scheduled for today
        </div>
    `;
}

// Display error message
function displayErrorMessage(message) {
    const trackerContainer = document.getElementById('trackerContainer');
    const statusContainer = document.getElementById('statusContainer');
    
    trackerContainer.innerHTML = `
        <div style="text-align: center; padding: 40px 20px; color: #888;">
            <div style="font-size: 48px; margin-bottom: 20px;">⚠️</div>
            <h2 style="color: #e0e0e0; margin-bottom: 10px;">Error Loading Timetable</h2>
            <p>Unable to load the timetable data.</p>
            <p style="margin-top: 20px; font-size: 14px; color: #666;">${message}</p>
        </div>
    `;
    
    statusContainer.innerHTML = `
        <div style="text-align: center; color: #888; padding: 20px;">
            Unable to load train status
        </div>
    `;
}

// Function to update derived data when timetable changes
function updateTimetableData() {
    // Re-extract stations
    stations.length = 0;
    stations.push(...extractStations());
    
    // Re-assign train colors
    Object.keys(trainColors).forEach(key => delete trainColors[key]);
    // Extract unique train numbers from timetable and sort alphabetically
    const uniqueTrains = [...new Set(timetableData.trains.map(t => t.trainNumber))].sort();
    // Assign colors based on alphabetical order
    uniqueTrains.forEach((trainNumber, index) => {
        trainColors[trainNumber] = colorPalette[index % colorPalette.length];
    });
}

// Extract stations dynamically from timetable data - we dont want to hardcode the list as they may change depending on the timetable 
function extractStations() {
    // Find the service with the most stops to get the complete station order
    
    let longestService = timetableData.trains[0];
    let maxStops = longestService.stops.length;
    
    for (let train of timetableData.trains) {
        if (train.stops.length > maxStops) {
            longestService = train;
            maxStops = train.stops.length;
        }
    }
    
    const stationsInRoute = longestService.stops.map(s => s.station);
    
    // If the longest service is northbound, reverse to get Kidderminster at top
    if (longestService.direction === 'northbound') {
        return stationsInRoute.reverse();
    }
    
    return stationsInRoute;
}

let stations = [];

// Assign unique colors to each unique train number dynamically from timetable
const trainColors = {};
const colorPalette = ['#ff6b6b', '#4ecdc4', '#45b7d1', '#96ceb4', '#ffeaa7', '#fd79a8', '#fdcb6e', '#6c5ce7', '#a29bfe', '#74b9ff'];

function getTrainColor(trainNumber) {
    return trainColors[trainNumber] || '#888888';
}

function parseTime(timeStr) {
    const [hours, minutes] = timeStr.split(':').map(Number);
    return hours * 60 + minutes;
}

function formatTime(minutes) {
    const h = Math.floor(minutes / 60);
    const m = minutes % 60;
    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

function getTrainIcon(trainNumber) {
    if (trainNumber.includes('Steam')) return '🚂';
    if (trainNumber.includes('DMU')) return '🚃';
    return '🚆';
}

// Manual time state - initialize to current time
const now = new Date();
let manualTime = now.getHours() * 60 + now.getMinutes();
let isLiveMode = true;

function updateTimeDisplay() {
    if (isLiveMode) {
        const now = new Date();
        const currentTime = now.getHours() * 60 + now.getMinutes();
        document.getElementById('timeDisplay').textContent = formatTime(currentTime);
    } else {
        document.getElementById('timeDisplay').textContent = formatTime(manualTime);
    }
}

function updateLiveButton() {
    const btn = document.getElementById('liveBtn');
    btn.disabled = isLiveMode;
}

function getCurrentTime() {
    if (isLiveMode) {
        const now = new Date();
        return now.getHours() * 60 + now.getMinutes();
    } else {
        return manualTime;
    }
}

function getStopTime(stop) {
    return parseTime(stop.departure || stop.arrival || stop.time);
}

function findTrainPosition(train, currentTime) {
    const stops = train.stops;
    
    // Check if train hasn't started yet
    const firstStop = stops[0];
    const startTime = getStopTime(firstStop);
    
    // Show train at terminal station 15 minutes before departure
    if (currentTime < startTime) {
        const isTerminalStation = firstStop.station === stations[0] || firstStop.station === stations[stations.length - 1];
        if (isTerminalStation && currentTime >= startTime - 15) {
            return {
                type: 'at_station',
                station: firstStop.station,
                waitingForDeparture: true
            };
        }
        return null;
    }
    
    // Check if train has completed journey
    const lastStop = stops[stops.length - 1];
    const endTime = getStopTime(lastStop);
    
    // If train has finished this service
    if (currentTime > endTime) {
        // Check if there's a future service for this train
        const lastStation = lastStop.station;
        const nextService = getNextService(train.trainNumber, lastStation, currentTime);
        
        if (nextService) {
            // Train is waiting at terminus for next service
            return {
                type: 'at_station',
                station: lastStation
            };
        } else {
            // No future service - remove after 15 minutes
            if (currentTime > endTime + 15) {
                return null;
            }
            return {
                type: 'at_station',
                station: lastStation
            };
        }
    }
    
    // Find current segment
    for (let i = 0; i < stops.length - 1; i++) {
        const currentStop = stops[i];
        const nextStop = stops[i + 1];
        
        const departTime = currentStop.departure ? parseTime(currentStop.departure) : getStopTime(currentStop);
        const arriveTime = nextStop.arrival ? parseTime(nextStop.arrival) : getStopTime(nextStop);
        
        if (currentTime >= departTime && currentTime <= arriveTime) {
            // Train is between stations
            const progress = (currentTime - departTime) / (arriveTime - departTime);
            return {
                type: 'between',
                fromStation: currentStop.station,
                toStation: nextStop.station,
                progress: progress,
                departTime: departTime,
                arriveTime: arriveTime
            };
        }
        
        // Check if at a station (between arrival and departure)
        if (currentStop.arrival && currentStop.departure) {
            const arrivalTime = parseTime(currentStop.arrival);
            const departureTime = parseTime(currentStop.departure);
            if (currentTime >= arrivalTime && currentTime < departureTime) {
                return {
                    type: 'at_station',
                    station: currentStop.station
                };
            }
        }
    }
    
    // Should not reach here, but return at final station as fallback
    return {
        type: 'at_station',
        station: lastStop.station
    };
}

function getNextService(trainNumber, currentStation, currentTime) {
    // Find any service with the same train number that departs from the current station
    const services = timetableData.trains.filter(t => t.trainNumber === trainNumber);
    
    for (let service of services) {
        const firstStop = service.stops[0];
        const startTime = getStopTime(firstStop);
        // Check that the service starts from the current station and is in the future
        if (startTime > currentTime && firstStop.station === currentStation) {
            return {
                service: service,
                departTime: startTime
            };
        }
    }
    return null;
}

function generateStatusText(train, position, currentTime) {
    const stops = train.stops;
    
    if (!position) {
        return null;
    }
    
    if (position.type === 'at_station') {
        const isTerminus = position.station === stations[0] || position.station === stations[stations.length - 1];
        
        // Check if this is a pre-departure waiting position
        if (position.waitingForDeparture) {
            const firstStop = stops[0];
            const departTime = getStopTime(firstStop);
            const minutesUntil = departTime - currentTime;
            return `Waiting at ${position.station}, departing at ${formatTime(departTime)} (departing in ${minutesUntil} minute${minutesUntil !== 1 ? 's' : ''})`;
        }
        
        if (isTerminus) {
            const nextService = getNextService(train.trainNumber, position.station, currentTime);
            if (nextService) {
                const minutesUntil = nextService.departTime - currentTime;
                return `Waiting at ${position.station}, next departure at ${formatTime(nextService.departTime)} (departing in ${minutesUntil} minute${minutesUntil !== 1 ? 's' : ''})`;
            } else {
                return `Terminated at ${position.station}`;
            }
        }
        
        // At intermediate station
        const currentStopIndex = stops.findIndex(s => s.station === position.station);
        const currentStop = stops[currentStopIndex];
        if (currentStop.departure) {
            const nextStop = stops[currentStopIndex + 1];
            const departTime = parseTime(currentStop.departure);
            const minutesUntil = departTime - currentTime;
            return `At ${position.station}, departing at ${currentStop.departure} (in ${minutesUntil} minute${minutesUntil !== 1 ? 's' : ''}) → ${nextStop.station}`;
        }
    }
    
    if (position.type === 'between') {
        const minutesUntilArrival = position.arriveTime - currentTime;
        const arrivalText = minutesUntilArrival === 0 ? 'arriving now' : `arriving ${formatTime(position.arriveTime)} in ${minutesUntilArrival} minute${minutesUntilArrival !== 1 ? 's' : ''}`;
        
        // Check for non-stop stations
        let statusParts = [`Traveling from ${position.fromStation} (departed ${formatTime(position.departTime)}) → ${position.toStation} (${arrivalText})`];
        
        // Find stations being passed without stopping
        const fromIndex = stations.indexOf(position.fromStation);
        const toIndex = stations.indexOf(position.toStation);
        const intermediateStations = train.direction === 'northbound' 
            ? stations.slice(fromIndex + 1, toIndex)
            : stations.slice(toIndex + 1, fromIndex);
        
        for (let station of intermediateStations) {
            const stopInfo = stops.find(s => s.station === station);
            if (stopInfo && !stopInfo.stopsAt) {
                const passTime = getStopTime(stopInfo);
                const minutesUntilPass = passTime - currentTime;
                if (minutesUntilPass > 0) {
                    statusParts.push(`Passing ${station} in ${minutesUntilPass} minute${minutesUntilPass !== 1 ? 's' : ''}`);
                }
            }
        }
        
        return statusParts.join(' • ');
    }
    
    return null;
}

function renderTracker() {
    if (!timetableData) return;
    
    const container = document.getElementById('trackerContainer');
    const currentTime = getCurrentTime();
    container.innerHTML = '';
    
    // Track active trains by train number only (not direction)
    const activeTrains = {};
    
    for (let train of timetableData.trains) {
        const position = findTrainPosition(train, currentTime);
        if (position) {
            const key = train.trainNumber;
            
            // If this train number already exists, we need to pick the right service
            if (activeTrains[key]) {
                const existingTrain = activeTrains[key].train;
                const existingPosition = activeTrains[key].position;
                
                const currentStartTime = getStopTime(train.stops[0]);
                const currentEndTime = getStopTime(train.stops[train.stops.length - 1]);
                const existingStartTime = getStopTime(existingTrain.stops[0]);
                const existingEndTime = getStopTime(existingTrain.stops[existingTrain.stops.length - 1]);
                
                // Check if services are currently running (between start and end)
                const currentIsRunning = currentTime >= currentStartTime && currentTime <= currentEndTime;
                const existingIsRunning = currentTime >= existingStartTime && currentTime <= existingEndTime;
                
                // Priority 1: Currently running service
                if (currentIsRunning && !existingIsRunning) {
                    activeTrains[key] = { train, position };
                }
                else if (!currentIsRunning && existingIsRunning) {
                    // Keep existing
                }
                // Both running: prioritize moving trains
                else if (currentIsRunning && existingIsRunning) {
                    if (position.type === 'between' && existingPosition.type === 'at_station') {
                        activeTrains[key] = { train, position };
                    }
                }
                // Neither running: both are waiting or pre-departure
                else {
                    // If current has finished and existing hasn't started (waiting for departure)
                    const currentHasFinished = currentTime > currentEndTime;
                    const existingHasFinished = currentTime > existingEndTime;
                    
                    if (currentHasFinished && !existingHasFinished) {
                        // Current service has finished, existing hasn't started - use current (it's waiting)
                        activeTrains[key] = { train, position };
                    }
                    else if (existingHasFinished && !currentHasFinished) {
                        // Keep existing (it's waiting)
                    }
                    else if (currentHasFinished && existingHasFinished) {
                        // Both finished - keep the most recent one
                        if (currentEndTime > existingEndTime) {
                            activeTrains[key] = { train, position };
                        }
                    }
                }
            } else {
                activeTrains[key] = { train, position };
            }
        }
    }
    
    // Render stations and tracks
    for (let i = 0; i < stations.length; i++) {
        const station = stations[i];
        
        // Create station div
        const stationDiv = document.createElement('div');
        stationDiv.className = 'station';
        stationDiv.innerHTML = `<div class="station-name">${station}</div>`;
        
        // Add trains at this station
        for (let key in activeTrains) {
            const { train, position } = activeTrains[key];
            if (position.type === 'at_station' && position.station === station) {
                const trainDiv = createTrainElement(train, 50);
                stationDiv.appendChild(trainDiv);
            }
        }
        
        container.appendChild(stationDiv);
        
        // Create track section (except after last station)
        if (i < stations.length - 1) {
            const trackDiv = document.createElement('div');
            trackDiv.className = 'track-section';
            trackDiv.innerHTML = '<div class="track-line"></div>';
            
            const nextStation = stations[i + 1];
            
            // Add trains between stations
            for (let key in activeTrains) {
                const { train, position } = activeTrains[key];
                if (position.type === 'between') {
                    let matchesSegment = false;
                    
                    if (train.direction === 'northbound') {
                        matchesSegment = position.fromStation === nextStation && position.toStation === station;
                    } else {
                        matchesSegment = position.fromStation === station && position.toStation === nextStation;
                    }
                    
                    if (matchesSegment) {
                        const percentage = train.direction === 'northbound' 
                            ? (1 - position.progress) * 100 
                            : position.progress * 100;
                        const trainDiv = createTrainElement(train, percentage);
                        trackDiv.appendChild(trainDiv);
                    }
                }
            }
            
            container.appendChild(trackDiv);
        }
    }
    
    // Render status panel
    renderStatus(activeTrains, currentTime);
}

function createTrainElement(train, topPercentage) {
    const trainDiv = document.createElement('div');
    trainDiv.className = `train ${train.direction}`;
    trainDiv.style.top = `${topPercentage}%`;
    trainDiv.style.transform = 'translateY(-50%)';
    
    const color = getTrainColor(train.trainNumber);
    trainDiv.style.backgroundColor = color + '33';
    trainDiv.style.borderLeft = `3px solid ${color}`;
    
    const icon = getTrainIcon(train.trainNumber);
    const arrow = train.direction === 'northbound' ? '↑' : '↓';
    
    trainDiv.innerHTML = `
        <div class="train-label">
            <span class="train-number">${train.trainNumber}</span>
        </div>
        <span class="train-icon">${icon}</span>
        <span class="direction-arrow">${arrow}</span>
    `;
    
    return trainDiv;
}

function renderStatus(activeTrains, currentTime) {
    const container = document.getElementById('statusContainer');
    container.innerHTML = '';
    
    if (Object.keys(activeTrains).length === 0) {
        container.innerHTML = '<div style="color: #888;">No trains currently active</div>';
        return;
    }
    
    for (let key in activeTrains) {
        const { train, position } = activeTrains[key];
        const statusText = generateStatusText(train, position, currentTime);
        
        if (statusText) {
            const statusDiv = document.createElement('div');
            statusDiv.className = 'train-status';
            const color = getTrainColor(train.trainNumber);
            statusDiv.style.borderLeftColor = color;
            
            statusDiv.innerHTML = `
                <div class="train-status-header">${train.trainNumber} (${train.direction})</div>
                <div class="train-status-detail">${statusText}</div>
            `;
            
            container.appendChild(statusDiv);
        }
    }
}

// Event listeners
document.getElementById('timeUp').addEventListener('click', () => {
    if (isLiveMode) {
        const now = new Date();
        manualTime = now.getHours() * 60 + now.getMinutes();
        isLiveMode = false;
    }
    manualTime += 5;
    if (manualTime >= 24 * 60) manualTime = 0;
    updateTimeDisplay();
    updateLiveButton();
    renderTracker();
    startAutoRefresh();
});

document.getElementById('timeDown').addEventListener('click', () => {
    if (isLiveMode) {
        const now = new Date();
        manualTime = now.getHours() * 60 + now.getMinutes();
        isLiveMode = false;
    }
    manualTime -= 5;
    if (manualTime < 0) manualTime = 24 * 60 - 5;
    updateTimeDisplay();
    updateLiveButton();
    renderTracker();
    startAutoRefresh();
});

document.getElementById('liveBtn').addEventListener('click', () => {
    isLiveMode = true;
    updateTimeDisplay();
    updateLiveButton();
    renderTracker();
    startAutoRefresh();
});

// Auto-refresh in live mode
let refreshInterval;

function startAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
    
    if (isLiveMode) {
        refreshInterval = setInterval(() => {
            updateTimeDisplay();
            renderTracker();
        }, 30000); // 30 seconds
    }
}

// Initial load
loadTimetable();
checkHealth();

// Check health status every 60 seconds
setInterval(checkHealth, 60000);
