import React, { useState, useEffect } from 'react';
import { Card, Typography, Tag, Space, Spin, Button } from 'antd';
import {
  EnvironmentOutlined, AlertOutlined, CheckCircleOutlined,
  ClockCircleOutlined, ReloadOutlined,
} from '@ant-design/icons';
import { MapContainer, TileLayer, Marker, Popup, useMap } from 'react-leaflet';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import api from '../../api/client';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../../store/AuthContext';

import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';

delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
});

const { Title, Text } = Typography;

const makeIcon = (color: string) =>
  L.divIcon({
    className: '',
    html: `<svg width="28" height="40" viewBox="0 0 28 40" xmlns="http://www.w3.org/2000/svg">
      <ellipse cx="14" cy="37" rx="6" ry="3" fill="rgba(0,0,0,0.2)"/>
      <path d="M14 0C6.27 0 0 6.27 0 14c0 10.5 14 26 14 26S28 24.5 28 14C28 6.27 21.73 0 14 0z" fill="${color}" stroke="white" stroke-width="1.5"/>
      <circle cx="14" cy="14" r="6" fill="white" opacity="0.9"/>
    </svg>`,
    iconSize: [28, 40],
    iconAnchor: [14, 40],
    popupAnchor: [0, -40],
  });

const icons = {
  red:    makeIcon('#ff4d4f'),
  yellow: makeIcon('#faad14'),
  blue:   makeIcon('#1890ff'),
  green:  makeIcon('#52c41a'),
};

type MapObject = {
  id: string;
  name: string;
  address: string;
  latitude: number;
  longitude: number;
  clientName: string;
  activeTicketCount: number;
  hasCriticalTicket: boolean;
  hasOverdueTicket: boolean;
};

type FilterMode = 'all' | 'critical' | 'overdue' | 'active' | 'ok';

const FitBounds: React.FC<{ objects: MapObject[] }> = ({ objects }) => {
  const map = useMap();
  useEffect(() => {
    if (objects.length === 0) return;
    const bounds = L.latLngBounds(objects.map(o => [o.latitude, o.longitude]));
    map.fitBounds(bounds, { padding: [40, 40], maxZoom: 14 });
  }, [objects]);
  return null;
};

const KARAGANDA_CENTER: [number, number] = [49.8047, 73.0858];
const KARAGANDA_ZOOM = 12;

const MapPage: React.FC = () => {
  const { user } = useAuth();
  const [objects, setObjects] = useState<MapObject[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<FilterMode>('all');
  const navigate = useNavigate();

  const isEngineer = user?.roles?.includes('Engineer') &&
    !user.roles.includes('Admin') &&
    !user.roles.includes('Manager');

  const fetchObjects = async () => {
    setLoading(true);
    try {
      const params: Record<string, string> = {};
      if (isEngineer && user?.id) {
        params.engineerUserId = user.id;
      }
      const res = await api.get('/geo/objects', { params });
      setObjects(res.data);
    } catch {

    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchObjects(); }, []);

  const filtered = objects.filter(o => {
    if (filter === 'critical') return o.hasCriticalTicket;
    if (filter === 'overdue')  return o.hasOverdueTicket && !o.hasCriticalTicket;
    if (filter === 'active')   return o.activeTicketCount > 0 && !o.hasCriticalTicket && !o.hasOverdueTicket;
    if (filter === 'ok')       return o.activeTicketCount === 0;
    return true;
  });

  const getIcon = (o: MapObject) => {
    if (o.hasCriticalTicket) return icons.red;
    if (o.hasOverdueTicket)  return icons.yellow;
    if (o.activeTicketCount > 0) return icons.blue;
    return icons.green;
  };

  const stats = {
    total:    objects.length,
    critical: objects.filter(o => o.hasCriticalTicket).length,
    overdue:  objects.filter(o => o.hasOverdueTicket && !o.hasCriticalTicket).length,
    active:   objects.filter(o => o.activeTicketCount > 0 && !o.hasCriticalTicket && !o.hasOverdueTicket).length,
    ok:       objects.filter(o => o.activeTicketCount === 0).length,
  };

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <Title level={2} style={{ margin: 0 }}>Карта объектов</Title>
        <Button icon={<ReloadOutlined />} onClick={fetchObjects} loading={loading}>
          Обновить
        </Button>
      </div>

      {isEngineer && (
        <div style={{ color: '#888', fontSize: 13 }}>
          Показаны только объекты с вашими активными заявками.
        </div>
      )}

      {}
      <Space wrap>
        <Card size="small" style={{ minWidth: 120, cursor: 'pointer', borderColor: filter === 'all' ? '#1890ff' : undefined }}
          onClick={() => setFilter('all')}>
          <Space><EnvironmentOutlined style={{ color: '#1890ff' }} /><Text>Все: <b>{stats.total}</b></Text></Space>
        </Card>
        <Card size="small" style={{ minWidth: 140, cursor: 'pointer', borderColor: filter === 'critical' ? '#ff4d4f' : undefined }}
          onClick={() => setFilter('critical')}>
          <Space><AlertOutlined style={{ color: '#ff4d4f' }} /><Text>Аварийные: <b>{stats.critical}</b></Text></Space>
        </Card>
        <Card size="small" style={{ minWidth: 160, cursor: 'pointer', borderColor: filter === 'overdue' ? '#faad14' : undefined }}
          onClick={() => setFilter('overdue')}>
          <Space><ClockCircleOutlined style={{ color: '#faad14' }} /><Text>Просроченные: <b>{stats.overdue}</b></Text></Space>
        </Card>
        <Card size="small" style={{ minWidth: 150, cursor: 'pointer', borderColor: filter === 'active' ? '#1890ff' : undefined }}
          onClick={() => setFilter('active')}>
          <Space><CheckCircleOutlined style={{ color: '#1890ff' }} /><Text>Активные: <b>{stats.active}</b></Text></Space>
        </Card>
        <Card size="small" style={{ minWidth: 130, cursor: 'pointer', borderColor: filter === 'ok' ? '#52c41a' : undefined }}
          onClick={() => setFilter('ok')}>
          <Space><CheckCircleOutlined style={{ color: '#52c41a' }} /><Text>Без заявок: <b>{stats.ok}</b></Text></Space>
        </Card>
      </Space>

      {}
      <Card bodyStyle={{ padding: 0, overflow: 'hidden', borderRadius: 8 }}>
        {loading ? (
          <div style={{ height: 600, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
            <Spin size="large" tip="Загрузка объектов..." />
          </div>
        ) : (
          <MapContainer
            center={KARAGANDA_CENTER}
            zoom={KARAGANDA_ZOOM}
            style={{ height: 600, width: '100%' }}
            scrollWheelZoom
          >
            <TileLayer
              attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
              url="https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png"
            />
            {filtered.length > 0 && <FitBounds objects={filtered} />}
            {filtered.map(obj => (
              <Marker
                key={obj.id}
                position={[obj.latitude, obj.longitude]}
                icon={getIcon(obj)}
              >
                <Popup>
                  <div style={{ minWidth: 200 }}>
                    <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 4 }}>{obj.name}</div>
                    <div style={{ color: '#666', fontSize: 12, marginBottom: 8 }}>{obj.clientName}</div>
                    <div style={{ fontSize: 12, marginBottom: 8, color: '#888' }}>{obj.address}</div>
                    {obj.activeTicketCount > 0 ? (
                      <Space direction="vertical" size={4}>
                        <Tag color="blue">Активных заявок: {obj.activeTicketCount}</Tag>
                        {obj.hasCriticalTicket && <Tag color="red">⚠ Критическая заявка</Tag>}
                        {obj.hasOverdueTicket  && <Tag color="orange">⏰ Просрочена</Tag>}
                      </Space>
                    ) : (
                      <Tag color="green">Заявок нет</Tag>
                    )}
                    <div style={{ marginTop: 10 }}>
                      <Button
                        size="small"
                        type="primary"
                        block
                        style={{ marginBottom: 6 }}
                        onClick={() => navigate(`/tickets?objectId=${obj.id}`)}
                      >
                        Заявки объекта
                      </Button>
                      <div style={{ display: 'flex', gap: 4 }}>
                        <Button
                          size="small"
                          block
                          style={{ fontSize: 11 }}
                          onClick={() =>
                            window.open(
                              `https://www.google.com/maps/dir/?api=1&destination=${obj.latitude},${obj.longitude}`,
                              '_blank'
                            )
                          }
                        >
                          📍 Google
                        </Button>
                        <Button
                          size="small"
                          block
                          style={{ fontSize: 11 }}
                          onClick={() =>
                            window.open(
                              `https://yandex.kz/maps/?rtext=~${obj.latitude},${obj.longitude}&rtt=auto`,
                              '_blank'
                            )
                          }
                        >
                          🧭 Яндекс
                        </Button>
                      </div>
                    </div>
                  </div>
                </Popup>
              </Marker>
            ))}
          </MapContainer>
        )}
      </Card>

      {}
      <Card size="small" title="Легенда">
        <Space wrap>
          <Space size={6}><div style={{ width: 14, height: 14, borderRadius: '50%', background: '#ff4d4f' }} /><Text>Критическая заявка</Text></Space>
          <Space size={6}><div style={{ width: 14, height: 14, borderRadius: '50%', background: '#faad14' }} /><Text>Просроченная заявка</Text></Space>
          <Space size={6}><div style={{ width: 14, height: 14, borderRadius: '50%', background: '#1890ff' }} /><Text>Активные заявки</Text></Space>
          <Space size={6}><div style={{ width: 14, height: 14, borderRadius: '50%', background: '#52c41a' }} /><Text>Без заявок</Text></Space>
        </Space>
      </Card>
    </div>
  );
};

export default MapPage;
